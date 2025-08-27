using System.Text;
using DotEnvX.Core.Models;
using DotEnvX.Core.Parser;
using DotEnvX.Core.Services;
using DotEnvX.Core.Helpers;
using DotEnvX.Core.Encryption;

namespace DotEnvX.Core;

public static class DotEnv
{
    private static readonly Logger Logger = new();
    
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
    
    public static Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null)
    {
        return DotEnvParser.Parse(src, options);
    }
    
    public static Dictionary<string, string> Parse(byte[] src, DotEnvParseOptions? options = null)
    {
        return DotEnvParser.Parse(src, options);
    }
    
    public static SetOutput Set(string key, string value, SetOptions? options = null)
    {
        options ??= new SetOptions();
        
        var setService = new SetService(key, value, options);
        return setService.Run();
    }
    
    public static string? Get(string key, GetOptions? options = null)
    {
        options ??= new GetOptions();
        
        var getService = new GetService(key, options);
        return getService.Run();
    }
    
    public static string[] Ls(string? directory = null, string[]? envFile = null, string[]? excludeEnvFile = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        envFile ??= new[] { ".env*", "*.env" };
        excludeEnvFile ??= new[] { ".env.keys", ".env.example", ".env.vault" };
        
        var lsService = new LsService(directory, envFile, excludeEnvFile);
        return lsService.Run();
    }
    
    public static GenExampleOutput GenExample(string? directory = null, string? envFile = null)
    {
        directory ??= Directory.GetCurrentDirectory();
        envFile ??= ".env";
        
        var genExampleService = new GenExampleService(directory, envFile);
        return genExampleService.Run();
    }
    
    public static DotEnvEncryption.KeyPair GenerateKeypair()
    {
        return DotEnvEncryption.GenerateKeyPair();
    }
    
    public static string Encrypt(string value, string publicKey)
    {
        return DotEnvEncryption.Encrypt(value, publicKey);
    }
    
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