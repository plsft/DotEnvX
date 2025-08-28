namespace DotEnvX.Core.Models;

/// <summary>
/// Options for configuring how .env files are loaded
/// </summary>
public class DotEnvOptions
{
    /// <summary>
    /// Array of .env file paths to load (defaults to [".env"])
    /// </summary>
    public string[]? Path { get; set; }
    
    /// <summary>
    /// File encoding (defaults to "utf-8")
    /// </summary>
    public string? Encoding { get; set; } = "utf-8";
    
    /// <summary>
    /// When true, overwrites existing environment variables (same as Override)
    /// </summary>
    public bool Overload { get; set; }
    
    /// <summary>
    /// When true, overwrites existing environment variables
    /// </summary>
    public bool Override { get; set; }
    
    /// <summary>
    /// When true, throws errors for missing files or other issues
    /// </summary>
    public bool Strict { get; set; }
    
    /// <summary>
    /// Array of error codes to ignore (e.g., "MISSING_ENV_FILE")
    /// </summary>
    public string[]? Ignore { get; set; }
    
    /// <summary>
    /// Process environment to use for variable expansion (defaults to current environment)
    /// </summary>
    public IDictionary<string, string>? ProcessEnv { get; set; }
    
    /// <summary>
    /// Path to file containing encryption keys (defaults to ".env.keys")
    /// </summary>
    public string? EnvKeysFile { get; set; }
    
    /// <summary>
    /// Encryption key for vault files
    /// </summary>
    public string? DotEnvKey { get; set; }
    
    /// <summary>
    /// Convention to use for loading files (e.g., "nextjs")
    /// </summary>
    public string? Convention { get; set; }
    
    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool Debug { get; set; }
    
    /// <summary>
    /// Enable verbose logging
    /// </summary>
    public bool Verbose { get; set; }
    
    /// <summary>
    /// Suppress all output
    /// </summary>
    public bool Quiet { get; set; }
    
    /// <summary>
    /// Logging level
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    
    /// <summary>
    /// Private key for decrypting encrypted values
    /// </summary>
    public string? PrivateKey { get; set; }
}

public enum LogLevel
{
    Error,
    Warn,
    Success,
    SuccessV,
    Info,
    Help,
    Verbose,
    Debug
}

/// <summary>
/// Result returned from DotEnv.Config operations
/// </summary>
public class DotEnvConfigResult
{
    /// <summary>
    /// Last error encountered during loading (null if no errors)
    /// </summary>
    public Exception? Error { get; set; }
    
    /// <summary>
    /// All parsed key-value pairs from loaded files
    /// </summary>
    public Dictionary<string, string>? Parsed { get; set; }
}

/// <summary>
/// Options for parsing .env file content
/// </summary>
public class DotEnvParseOptions
{
    /// <summary>
    /// When true, overwrites existing environment variables
    /// </summary>
    public bool Overload { get; set; }
    
    /// <summary>
    /// When true, overwrites existing environment variables
    /// </summary>
    public bool Override { get; set; }
    
    /// <summary>
    /// Process environment to use for variable expansion
    /// </summary>
    public IDictionary<string, string>? ProcessEnv { get; set; }
    
    /// <summary>
    /// Private key for decrypting encrypted values
    /// </summary>
    public string? PrivateKey { get; set; }
}

/// <summary>
/// Options for setting environment variables in .env files
/// </summary>
public class SetOptions
{
    /// <summary>
    /// Array of .env file paths to update (defaults to [".env"])
    /// </summary>
    public string[]? Path { get; set; }
    
    /// <summary>
    /// Path to file containing encryption keys
    /// </summary>
    public string? EnvKeysFile { get; set; }
    
    /// <summary>
    /// Convention to use (e.g., "nextjs")
    /// </summary>
    public string? Convention { get; set; }
    
    /// <summary>
    /// When true, encrypts the value before saving (defaults to false)
    /// </summary>
    public bool Encrypt { get; set; } = false;
}

/// <summary>
/// Options for getting environment variable values
/// </summary>
public class GetOptions
{
    /// <summary>
    /// Error codes to ignore
    /// </summary>
    public string[]? Ignore { get; set; }
    
    /// <summary>
    /// When true, overwrites existing environment variables
    /// </summary>
    public bool Overload { get; set; }
    
    /// <summary>
    /// Path to file containing encryption keys
    /// </summary>
    public string? EnvKeysFile { get; set; }
    
    /// <summary>
    /// When true, throws errors for missing values
    /// </summary>
    public bool Strict { get; set; }
}

public class ProcessedEnv
{
    public string? Type { get; set; }
    public string? Filepath { get; set; }
    public string? EnvFilepath { get; set; }
    public Dictionary<string, string>? Parsed { get; set; }
    public Dictionary<string, string>? Injected { get; set; }
    public Dictionary<string, string>? PreExisted { get; set; }
    public List<DotEnvError>? Errors { get; set; }
    public string? EnvSrc { get; set; }
    public string? PrivateKey { get; set; }
    public string? PublicKey { get; set; }
}

public class SetProcessedEnv
{
    public required string Key { get; set; }
    public required string Value { get; set; }
    public required string Filepath { get; set; }
    public required string EnvFilepath { get; set; }
    public required string EnvSrc { get; set; }
    public bool Changed { get; set; }
    public string? EncryptedValue { get; set; }
    public string? PublicKey { get; set; }
    public string? PrivateKey { get; set; }
    public bool PrivateKeyAdded { get; set; }
    public string? PrivateKeyName { get; set; }
    public Exception? Error { get; set; }
}

public class SetOutput
{
    public required List<SetProcessedEnv> ProcessedEnvs { get; set; }
    public required List<string> ChangedFilepaths { get; set; }
    public required List<string> UnchangedFilepaths { get; set; }
}

public class GenExampleOutput
{
    public required string EnvExampleFile { get; set; }
    public required string[] EnvFile { get; set; }
    public required string ExampleFilepath { get; set; }
    public required List<string> AddedKeys { get; set; }
    public required Dictionary<string, string> Injected { get; set; }
    public required Dictionary<string, string> PreExisted { get; set; }
}