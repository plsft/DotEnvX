using System.Text;
using DotEnvX.Core.Models;
using DotEnvX.Core.Parser;
using DotEnvX.Core.Services;
using DotEnvX.Core.Helpers;
using DotEnvX.Core.Encryption;

namespace DotEnvX.Core;

/// <summary>
/// Main class for loading and managing environment variables from .env files
/// </summary>
public static class DotEnv
{
    private static readonly Logger Logger = new();
    
    /// <summary>
    /// Loads environment variables from .env file(s) into the process environment
    /// </summary>
    /// <param name="options">Configuration options for loading .env files</param>
    /// <returns>Result containing parsed values and any errors encountered</returns>
    /// <example>
    /// <code>
    /// // Simple usage
    /// var result = DotEnv.Config();
    /// 
    /// // With options
    /// var result = DotEnv.Config(new DotEnvOptions
    /// {
    ///     Path = new[] { ".env", ".env.local" },
    ///     Overload = true,
    ///     Strict = false
    /// });
    /// </code>
    /// </example>
    public static DotEnvConfigResult Config(DotEnvOptions? options = null)
    {
        options ??= new DotEnvOptions();
        
        // Set up logger
        Logger.SetLogLevel(options.LogLevel);
        Logger.SetQuiet(options.Quiet);
        Logger.SetVerbose(options.Verbose);
        Logger.SetDebug(options.Debug);
        
        try
        {
            // Get process environment
            var processEnv = options.ProcessEnv ?? ToDictionary(Environment.GetEnvironmentVariables());
            
            // Build environment configurations
            var envs = EnvBuilder.BuildEnvs(options);
            
            // Run the configuration
            var runner = new RunService(envs, options, processEnv);
            var runResult = runner.Run();
            
            var parsed = new Dictionary<string, string>();
            Exception? lastError = null;
            
            foreach (var processedEnv in runResult.ProcessedEnvs)
            {
                // Log based on type
                if (processedEnv.Type == "envVaultFile")
                {
                    Logger.Verbose($"Loading env from encrypted {processedEnv.Filepath}");
                    Logger.Debug($"Decrypting encrypted env from {processedEnv.Filepath}");
                }
                else if (processedEnv.Type == "envFile")
                {
                    Logger.Verbose($"Loading env from {processedEnv.Filepath}");
                }
                
                // Handle errors
                if (processedEnv.Errors != null)
                {
                    foreach (var error in processedEnv.Errors)
                    {
                        if (options.Ignore?.Contains(error.Code) == true)
                        {
                            Logger.Verbose($"Ignored: {error.Message}");
                            continue;
                        }
                        
                        if (options.Strict)
                        {
                            throw error;
                        }
                        
                        lastError = error;
                        
                        if (error.Code == ErrorCodes.MISSING_ENV_FILE && options.Convention == null)
                        {
                            Logger.Error(error.Message);
                            if (!string.IsNullOrEmpty(error.Help))
                            {
                                Logger.Error(error.Help);
                            }
                        }
                        else if (error.Code != ErrorCodes.MISSING_ENV_FILE)
                        {
                            Logger.Error(error.Message);
                            if (!string.IsNullOrEmpty(error.Help))
                            {
                                Logger.Error(error.Help);
                            }
                        }
                    }
                }
                
                // Add parsed values
                if (processedEnv.Parsed != null)
                {
                    foreach (var kvp in processedEnv.Parsed)
                    {
                        parsed[kvp.Key] = kvp.Value;
                    }
                }
                
                // Apply to process environment
                if (processedEnv.Injected != null)
                {
                    foreach (var kvp in processedEnv.Injected)
                    {
                        Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
                        Logger.Verbose($"Set {kvp.Key}");
                    }
                }
            }
            
            // Log injection summary
            if (runResult.UniqueInjectedKeys.Any())
            {
                Logger.Success($"Loaded {string.Join(", ", runResult.UniqueInjectedKeys)}");
            }
            
            return new DotEnvConfigResult
            {
                Parsed = parsed,
                Error = lastError
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"Config failed: {ex.Message}");
            return new DotEnvConfigResult
            {
                Error = ex
            };
        }
    }
    
    /// <summary>
    /// Parses .env file content into a dictionary without loading into environment
    /// </summary>
    /// <param name="src">The .env file content as a string</param>
    /// <param name="options">Parsing options</param>
    /// <returns>Dictionary of parsed key-value pairs</returns>
    public static Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null)
    {
        return DotEnvParser.Parse(src, options);
    }
    
    /// <summary>
    /// Parses .env file content from byte array into a dictionary without loading into environment
    /// </summary>
    /// <param name="src">The .env file content as bytes</param>
    /// <param name="options">Parsing options</param>
    /// <returns>Dictionary of parsed key-value pairs</returns>
    public static Dictionary<string, string> Parse(byte[] src, DotEnvParseOptions? options = null)
    {
        return DotEnvParser.Parse(src, options);
    }
    
    /// <summary>
    /// Sets an environment variable in .env file(s)
    /// </summary>
    /// <param name="key">Environment variable name</param>
    /// <param name="value">Value to set</param>
    /// <param name="options">Options for setting the value (encryption, file path, etc.)</param>
    /// <returns>Result containing information about the operation</returns>
    /// <example>
    /// <code>
    /// // Set plain value
    /// DotEnv.Set("API_URL", "https://api.example.com");
    /// 
    /// // Set encrypted value
    /// DotEnv.Set("API_SECRET", "secret-value", new SetOptions { Encrypt = true });
    /// </code>
    /// </example>
    public static SetOutput Set(string key, string value, SetOptions? options = null)
    {
        options ??= new SetOptions();
        
        var setService = new SetService(key, value, options);
        return setService.Run();
    }
    
    /// <summary>
    /// Gets the value of an environment variable from .env file(s)
    /// </summary>
    /// <param name="key">Environment variable name</param>
    /// <param name="options">Options for getting the value</param>
    /// <returns>The value of the environment variable, or null if not found</returns>
    public static string? Get(string key, GetOptions? options = null)
    {
        options ??= new GetOptions();
        
        var getService = new GetService(key, options);
        return getService.Run();
    }
    
    /// <summary>
    /// Lists all .env files in a directory
    /// </summary>
    /// <param name="directory">Directory to search (defaults to current directory)</param>
    /// <param name="envFile">Patterns to include (defaults to .env* and *.env)</param>
    /// <param name="excludeEnvFile">Patterns to exclude (defaults to .env.keys, .env.example, .env.vault)</param>
    /// <returns>Array of .env file paths found</returns>
    public static string[] Ls(string? directory = null, string[]? envFile = null, string[]? excludeEnvFile = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        envFile ??= new[] { ".env*", "*.env" };
        excludeEnvFile ??= new[] { ".env.keys", ".env.example", ".env.vault" };
        
        var lsService = new LsService(directory, envFile, excludeEnvFile);
        return lsService.Run();
    }
    
    /// <summary>
    /// Generates a .env.example file from an existing .env file
    /// </summary>
    /// <param name="directory">Directory containing the .env file</param>
    /// <param name="envFile">Path to the .env file</param>
    /// <returns>Result containing information about the generated example file</returns>
    public static GenExampleOutput GenExample(string? directory = null, string? envFile = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        envFile ??= ".env";
        
        var genExampleService = new GenExampleService(directory, envFile);
        return genExampleService.Run();
    }
    
    /// <summary>
    /// Generates a new public/private key pair for encryption
    /// </summary>
    /// <returns>A KeyPair containing the public and private keys</returns>
    /// <example>
    /// <code>
    /// var keypair = DotEnv.GenerateKeypair();
    /// File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}");
    /// File.AppendAllText(".env", $"#DOTENV_PUBLIC_KEY={keypair.PublicKey}\n");
    /// </code>
    /// </example>
    public static DotEnvEncryption.KeyPair GenerateKeypair()
    {
        return DotEnvEncryption.GenerateKeyPair();
    }
    
    /// <summary>
    /// Encrypts a value using a public key
    /// </summary>
    /// <param name="value">The plain text value to encrypt</param>
    /// <param name="publicKey">The public key to use for encryption</param>
    /// <returns>The encrypted value with "encrypted:" prefix</returns>
    public static string Encrypt(string value, string publicKey)
    {
        return DotEnvEncryption.Encrypt(value, publicKey);
    }
    
    /// <summary>
    /// Decrypts a value using a private key
    /// </summary>
    /// <param name="encryptedValue">The encrypted value (with or without "encrypted:" prefix)</param>
    /// <param name="privateKey">The private key to use for decryption</param>
    /// <returns>The decrypted plain text value</returns>
    public static string Decrypt(string encryptedValue, string privateKey)
    {
        return DotEnvEncryption.Decrypt(encryptedValue, privateKey);
    }
    
    private static Dictionary<string, string> ToDictionary(System.Collections.IDictionary dict)
    {
        var result = new Dictionary<string, string>();
        foreach (var key in dict.Keys)
        {
            if (key != null && dict[key] != null)
            {
                result[key.ToString()!] = dict[key]!.ToString()!;
            }
        }
        return result;
    }
}