namespace DotEnvX.Core.Models;

public class DotEnvError : Exception
{
    public string Code { get; }
    public string? Help { get; }

    public DotEnvError(string code, string message, string? help = null) : base(message)
    {
        Code = code;
        Help = help;
    }
}

public static class ErrorCodes
{
    public const string MISSING_ENV_FILE = "MISSING_ENV_FILE";
    public const string MISSING_ENV_VAULT_FILE = "MISSING_ENV_VAULT_FILE";
    public const string DECRYPTION_FAILED = "DECRYPTION_FAILED";
    public const string INVALID_DOTENV_KEY = "INVALID_DOTENV_KEY";
    public const string MISSING_DOTENV_KEY = "MISSING_DOTENV_KEY";
    public const string MISSING_PRIVATE_KEY = "MISSING_PRIVATE_KEY";
    public const string MISSING_PUBLIC_KEY = "MISSING_PUBLIC_KEY";
    public const string INVALID_PRIVATE_KEY = "INVALID_PRIVATE_KEY";
    public const string INVALID_PUBLIC_KEY = "INVALID_PUBLIC_KEY";
    public const string ENV_FILE_WRITE_ERROR = "ENV_FILE_WRITE_ERROR";
    public const string KEY_NOT_FOUND = "KEY_NOT_FOUND";
    public const string ALREADY_EXISTS = "ALREADY_EXISTS";
}