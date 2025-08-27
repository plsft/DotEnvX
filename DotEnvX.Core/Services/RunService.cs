using System.Text;
using DotEnvX.Core.Models;
using DotEnvX.Core.Parser;
using DotEnvX.Core.Helpers;
using DotEnvX.Core.Encryption;

namespace DotEnvX.Core.Services;

public class RunService
{
    private readonly List<EnvConfig> _envs;
    private readonly DotEnvOptions _options;
    private readonly Dictionary<string, string> _processEnv;
    private readonly Dictionary<string, string> _beforeEnv;
    
    public RunService(List<EnvConfig> envs, DotEnvOptions options, IDictionary<string, string> processEnv)
    {
        _envs = envs;
        _options = options;
        _processEnv = new Dictionary<string, string>(processEnv);
        _beforeEnv = new Dictionary<string, string>(processEnv);
    }
    
    public RunResult Run()
    {
        var processedEnvs = new List<ProcessedEnv>();
        var uniqueInjectedKeys = new HashSet<string>();
        var afterEnv = new Dictionary<string, string>(_beforeEnv);
        
        foreach (var env in _envs)
        {
            ProcessedEnv processedEnv;
            
            if (env.Type == "envVaultFile")
            {
                processedEnv = ProcessVaultFile(env.Value);
            }
            else
            {
                processedEnv = ProcessEnvFile(env.Value);
            }
            
            processedEnvs.Add(processedEnv);
            
            // Apply to environment
            if (processedEnv.Injected != null)
            {
                foreach (var kvp in processedEnv.Injected)
                {
                    afterEnv[kvp.Key] = kvp.Value;
                    _processEnv[kvp.Key] = kvp.Value;
                    uniqueInjectedKeys.Add(kvp.Key);
                }
            }
        }
        
        return new RunResult
        {
            ProcessedEnvs = processedEnvs,
            BeforeEnv = _beforeEnv,
            AfterEnv = afterEnv,
            UniqueInjectedKeys = uniqueInjectedKeys.ToList(),
            ReadableFilepaths = processedEnvs
                .Where(p => p.Errors == null || !p.Errors.Any())
                .Select(p => p.Filepath!)
                .Where(f => f != null)
                .ToList()
        };
    }
    
    private ProcessedEnv ProcessEnvFile(string filepath)
    {
        var processedEnv = new ProcessedEnv
        {
            Type = "envFile",
            Filepath = Path.GetFullPath(filepath),
            EnvFilepath = filepath,
            Errors = new List<DotEnvError>()
        };
        
        try
        {
            if (!File.Exists(filepath))
            {
                processedEnv.Errors.Add(new DotEnvError(
                    ErrorCodes.MISSING_ENV_FILE,
                    $"Missing env file: {filepath}",
                    "Create the file or add it to .gitignore if intentionally missing"
                ));
                return processedEnv;
            }
            
            // Read file
            var encoding = DetectEncoding(filepath);
            var content = File.ReadAllText(filepath, encoding);
            processedEnv.EnvSrc = content;
            
            // Check for private key in .env.keys file
            string? privateKey = null;
            if (!string.IsNullOrEmpty(_options.EnvKeysFile) && File.Exists(_options.EnvKeysFile))
            {
                privateKey = FindPrivateKeyForFile(filepath, _options.EnvKeysFile);
            }
            else if (File.Exists(".env.keys"))
            {
                privateKey = FindPrivateKeyForFile(filepath, ".env.keys");
            }
            
            privateKey ??= _options.PrivateKey;
            
            // Parse content
            var parseOptions = new DotEnvParseOptions
            {
                Overload = _options.Overload,
                Override = _options.Override,
                ProcessEnv = _processEnv,
                PrivateKey = privateKey
            };
            
            var parsed = DotEnvParser.Parse(content, parseOptions);
            processedEnv.Parsed = parsed;
            
            // Determine what to inject
            var injected = new Dictionary<string, string>();
            var preExisted = new Dictionary<string, string>();
            
            foreach (var kvp in parsed)
            {
                if (_processEnv.ContainsKey(kvp.Key))
                {
                    if (_options.Override || _options.Overload)
                    {
                        injected[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        preExisted[kvp.Key] = _processEnv[kvp.Key];
                    }
                }
                else
                {
                    injected[kvp.Key] = kvp.Value;
                }
            }
            
            processedEnv.Injected = injected;
            processedEnv.PreExisted = preExisted;
            processedEnv.PrivateKey = privateKey;
        }
        catch (Exception ex)
        {
            processedEnv.Errors.Add(new DotEnvError(
                "PROCESSING_ERROR",
                $"Error processing {filepath}: {ex.Message}"
            ));
        }
        
        return processedEnv;
    }
    
    private ProcessedEnv ProcessVaultFile(string filepath)
    {
        var processedEnv = new ProcessedEnv
        {
            Type = "envVaultFile",
            Filepath = Path.GetFullPath(filepath),
            EnvFilepath = filepath,
            Errors = new List<DotEnvError>()
        };
        
        try
        {
            if (!File.Exists(filepath))
            {
                processedEnv.Errors.Add(new DotEnvError(
                    ErrorCodes.MISSING_ENV_VAULT_FILE,
                    $"Missing vault file: {filepath}",
                    "Run 'dotenvx encrypt' to create a vault file"
                ));
                return processedEnv;
            }
            
            // Get DOTENV_KEY
            var dotenvKey = _options.DotEnvKey ?? Environment.GetEnvironmentVariable("DOTENV_KEY");
            if (string.IsNullOrEmpty(dotenvKey))
            {
                processedEnv.Errors.Add(new DotEnvError(
                    ErrorCodes.MISSING_DOTENV_KEY,
                    "DOTENV_KEY is not set",
                    "Set DOTENV_KEY environment variable or pass it in options"
                ));
                return processedEnv;
            }
            
            // Parse DOTENV_KEY to extract environment and key
            var (environment, decryptionKey) = ParseDotEnvKey(dotenvKey);
            
            // Read vault file
            var vaultContent = File.ReadAllText(filepath);
            var vaultData = DotEnvParser.Parse(vaultContent, new DotEnvParseOptions());
            
            // Find the encrypted content for the environment
            var envKey = $"DOTENV_VAULT_{environment.ToUpper()}";
            if (!vaultData.TryGetValue(envKey, out var encryptedContent))
            {
                processedEnv.Errors.Add(new DotEnvError(
                    ErrorCodes.DECRYPTION_FAILED,
                    $"Environment '{environment}' not found in vault",
                    $"Available environments: {string.Join(", ", vaultData.Keys.Where(k => k.StartsWith("DOTENV_VAULT_")))}"
                ));
                return processedEnv;
            }
            
            // Decrypt the content
            var decrypted = DotEnvEncryption.Decrypt(encryptedContent, decryptionKey);
            processedEnv.EnvSrc = decrypted;
            
            // Parse decrypted content
            var parseOptions = new DotEnvParseOptions
            {
                Overload = _options.Overload,
                Override = _options.Override,
                ProcessEnv = _processEnv
            };
            
            var parsed = DotEnvParser.Parse(decrypted, parseOptions);
            processedEnv.Parsed = parsed;
            
            // Determine what to inject
            var injected = new Dictionary<string, string>();
            var preExisted = new Dictionary<string, string>();
            
            foreach (var kvp in parsed)
            {
                if (_processEnv.ContainsKey(kvp.Key))
                {
                    if (_options.Override || _options.Overload)
                    {
                        injected[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        preExisted[kvp.Key] = _processEnv[kvp.Key];
                    }
                }
                else
                {
                    injected[kvp.Key] = kvp.Value;
                }
            }
            
            processedEnv.Injected = injected;
            processedEnv.PreExisted = preExisted;
        }
        catch (Exception ex)
        {
            processedEnv.Errors.Add(new DotEnvError(
                ErrorCodes.DECRYPTION_FAILED,
                $"Error processing vault {filepath}: {ex.Message}"
            ));
        }
        
        return processedEnv;
    }
    
    private static Encoding DetectEncoding(string filepath)
    {
        var bytes = File.ReadAllBytes(filepath);
        
        // Check for BOM
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8;
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode;
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode;
        
        // Default to UTF8
        return Encoding.UTF8;
    }
    
    private static string? FindPrivateKeyForFile(string envFile, string keysFile)
    {
        if (!File.Exists(keysFile))
            return null;
            
        var keysContent = File.ReadAllText(keysFile);
        var keys = DotEnvParser.Parse(keysContent, new DotEnvParseOptions());
        
        // Try to find matching key
        var envName = Path.GetFileNameWithoutExtension(envFile).Replace(".env", "").TrimStart('.');
        var keyName = string.IsNullOrEmpty(envName) ? "DOTENV_PRIVATE_KEY" : $"DOTENV_PRIVATE_KEY_{envName.ToUpper()}";
        
        return keys.TryGetValue(keyName, out var key) ? key : null;
    }
    
    private static (string environment, string key) ParseDotEnvKey(string dotenvKey)
    {
        // Format: dotenv://:key_xxx@dotenvx.com/vault/.env.vault?environment=production
        try
        {
            var uri = new Uri(dotenvKey);
            var environment = System.Web.HttpUtility.ParseQueryString(uri.Query)["environment"] ?? "production";
            var key = uri.UserInfo.Split(':')[1];
            return (environment, key);
        }
        catch
        {
            // Fallback: assume it's just the key
            return ("production", dotenvKey);
        }
    }
}

public class RunResult
{
    public required List<ProcessedEnv> ProcessedEnvs { get; set; }
    public required Dictionary<string, string> BeforeEnv { get; set; }
    public required Dictionary<string, string> AfterEnv { get; set; }
    public required List<string> UniqueInjectedKeys { get; set; }
    public required List<string> ReadableFilepaths { get; set; }
}