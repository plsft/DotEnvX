namespace DotEnvX.Core.Models;

public class DotEnvOptions
{
    public string[]? Path { get; set; }
    public string? Encoding { get; set; } = "utf-8";
    public bool Overload { get; set; }
    public bool Override { get; set; }
    public bool Strict { get; set; }
    public string[]? Ignore { get; set; }
    public IDictionary<string, string>? ProcessEnv { get; set; }
    public string? EnvKeysFile { get; set; }
    public string? DotEnvKey { get; set; }
    public string? Convention { get; set; }
    public bool Debug { get; set; }
    public bool Verbose { get; set; }
    public bool Quiet { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
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

public class DotEnvConfigResult
{
    public Exception? Error { get; set; }
    public Dictionary<string, string>? Parsed { get; set; }
}

public class DotEnvParseOptions
{
    public bool Overload { get; set; }
    public bool Override { get; set; }
    public IDictionary<string, string>? ProcessEnv { get; set; }
    public string? PrivateKey { get; set; }
}

public class SetOptions
{
    public string[]? Path { get; set; }
    public string? EnvKeysFile { get; set; }
    public string? Convention { get; set; }
    public bool Encrypt { get; set; } = true;
}

public class GetOptions
{
    public string[]? Ignore { get; set; }
    public bool Overload { get; set; }
    public string? EnvKeysFile { get; set; }
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