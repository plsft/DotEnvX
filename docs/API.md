# DotEnvX API Documentation

## Table of Contents

- [DotEnv Static Class](#dotenv-static-class)
- [Options Classes](#options-classes)
- [Service Interfaces](#service-interfaces)
- [Models](#models)
- [Encryption](#encryption)
- [Dependency Injection](#dependency-injection)
- [Error Handling](#error-handling)
- [Best Practices](#best-practices)

## DotEnv Static Class

The main entry point for the DotEnvX library.

### Config

Loads environment variables from .env files into the process environment.

```csharp
public static DotEnvConfigResult Config(DotEnvOptions? options = null)
```

**Parameters:**
- `options` - Configuration options (optional)

**Returns:** `DotEnvConfigResult` containing parsed variables and any errors

**Example:**
```csharp
// Simple usage
var result = DotEnv.Config();

// With options
var result = DotEnv.Config(new DotEnvOptions
{
    Path = new[] { ".env", ".env.production" },
    Overload = true,
    Strict = false
});

if (result.Error != null)
{
    Console.WriteLine($"Error: {result.Error.Message}");
}
```

### Parse

Parses .env content into a dictionary without loading into the environment.

```csharp
public static Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null)
public static Dictionary<string, string> Parse(byte[] src, DotEnvParseOptions? options = null)
```

**Parameters:**
- `src` - The .env file content to parse (string or byte array)
- `options` - Parse options (optional)

**Returns:** Dictionary of parsed key-value pairs

**Example:**
```csharp
var content = File.ReadAllText(".env");
var vars = DotEnv.Parse(content, new DotEnvParseOptions
{
    ProcessEnv = Environment.GetEnvironmentVariables().Cast<DictionaryEntry>()
        .ToDictionary(e => e.Key.ToString(), e => e.Value?.ToString() ?? "")
});
```

### Set

Sets an environment variable in a .env file.

```csharp
public static SetOutput Set(string key, string value, SetOptions? options = null)
```

**Parameters:**
- `key` - Variable name
- `value` - Variable value
- `options` - Set options (optional)

**Returns:** `SetOutput` with operation results

**Example:**
```csharp
// Set plain value
var output = DotEnv.Set("API_KEY", "secret123");

// Set encrypted value
var output = DotEnv.Set("API_SECRET", "super-secret", new SetOptions
{
    Path = new[] { ".env" },
    Encrypt = true
});
```

### Get

Retrieves an environment variable value from .env files.

```csharp
public static string? Get(string key, GetOptions? options = null)
```

**Parameters:**
- `key` - Variable name
- `options` - Get options (optional)

**Returns:** Variable value or null if not found

**Example:**
```csharp
var apiKey = DotEnv.Get("API_KEY");
```

### GenerateKeypair

Generates an encryption keypair for ECIES encryption.

```csharp
public static DotEnvEncryption.KeyPair GenerateKeypair()
```

**Returns:** `KeyPair` with public and private keys

**Example:**
```csharp
var keypair = DotEnv.GenerateKeypair();
File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}");
File.AppendAllText(".env", $"#DOTENV_PUBLIC_KEY={keypair.PublicKey}\n");
```

### Encrypt

Encrypts a value using a public key.

```csharp
public static string Encrypt(string value, string publicKey)
```

**Parameters:**
- `value` - Value to encrypt
- `publicKey` - Public key for encryption (hex format)

**Returns:** Encrypted value with "encrypted:" prefix

**Example:**
```csharp
var encrypted = DotEnv.Encrypt("secret", publicKey);
// Returns: "encrypted:BDb7t3QkTRp2..."
```

### Decrypt

Decrypts a value using a private key.

```csharp
public static string Decrypt(string encryptedValue, string privateKey)
```

**Parameters:**
- `encryptedValue` - Encrypted value (with or without "encrypted:" prefix)
- `privateKey` - Private key for decryption (hex format)

**Returns:** Decrypted value string

**Example:**
```csharp
var decrypted = DotEnv.Decrypt("encrypted:BDb7t3QkTRp2...", privateKey);
```

### Ls

Lists all .env files in a directory.

```csharp
public static string[] Ls(string? directory = null, string[]? envFile = null, string[]? excludeEnvFile = null)
```

**Parameters:**
- `directory` - Directory to search (defaults to current)
- `envFile` - Patterns to include (defaults to ".env*", "*.env")
- `excludeEnvFile` - Patterns to exclude (defaults to ".env.keys", ".env.example", ".env.vault")

**Returns:** Array of .env file paths

### GenExample

Generates a .env.example file from an existing .env file.

```csharp
public static GenExampleOutput GenExample(string? directory = null, string? envFile = null)
```

**Parameters:**
- `directory` - Directory containing the .env file
- `envFile` - Path to the .env file

**Returns:** `GenExampleOutput` with generation results

## Options Classes

### DotEnvOptions

Configuration options for loading .env files.

```csharp
public class DotEnvOptions
{
    public string[]? Path { get; set; }           // Files to load (default: [".env"])
    public string? Encoding { get; set; }         // File encoding (default: "utf-8")
    public bool Overload { get; set; }            // Override existing vars
    public bool Override { get; set; }            // Override existing vars (alias)
    public bool Strict { get; set; }              // Throw on missing files
    public string[]? Ignore { get; set; }         // Error codes to ignore
    public IDictionary<string, string>? ProcessEnv { get; set; } // Process environment
    public string? EnvKeysFile { get; set; }      // Path to keys file (default: ".env.keys")
    public string? DotEnvKey { get; set; }        // Vault encryption key
    public string? Convention { get; set; }       // Convention to use (e.g., "nextjs")
    public bool Debug { get; set; }               // Enable debug logging
    public bool Verbose { get; set; }             // Enable verbose logging
    public bool Quiet { get; set; }               // Suppress all output
    public LogLevel LogLevel { get; set; }        // Logging level
    public string? PrivateKey { get; set; }       // Private key for decryption
}
```

### DotEnvParseOptions

Options for parsing .env content.

```csharp
public class DotEnvParseOptions
{
    public bool Overload { get; set; }                          // Override existing vars
    public bool Override { get; set; }                          // Override existing vars
    public IDictionary<string, string>? ProcessEnv { get; set; } // Vars for expansion
    public string? PrivateKey { get; set; }                     // For decryption
}
```

### SetOptions

Options for setting variables.

```csharp
public class SetOptions
{
    public string[]? Path { get; set; }      // Target files (default: [".env"])
    public string? EnvKeysFile { get; set; } // Path to keys file
    public string? Convention { get; set; }  // Convention to use
    public bool Encrypt { get; set; }        // Encrypt value (default: false)
}
```

### GetOptions

Options for retrieving variables.

```csharp
public class GetOptions
{
    public string[]? Ignore { get; set; }    // Error codes to ignore
    public bool Overload { get; set; }       // Override existing vars
    public string? EnvKeysFile { get; set; } // Path to keys file
    public bool Strict { get; set; }         // Throw on missing values
}
```

## Models

### DotEnvConfigResult

Result of loading .env files.

```csharp
public class DotEnvConfigResult
{
    public Exception? Error { get; set; }                // Last error encountered
    public Dictionary<string, string>? Parsed { get; set; } // All parsed variables
}
```

### DotEnvEncryption.KeyPair

Encryption keypair.

```csharp
public class KeyPair
{
    public string PublicKey { get; set; }   // Public key (hex)
    public string PrivateKey { get; set; }  // Private key (hex)
}
```

### SetOutput

Result of set operation.

```csharp
public class SetOutput
{
    public List<SetProcessedEnv> ProcessedEnvs { get; set; } // Processed environments
    public List<string> ChangedFilepaths { get; set; }       // Modified files
    public List<string> UnchangedFilepaths { get; set; }     // Unchanged files
}
```

### GenExampleOutput

Result of example generation.

```csharp
public class GenExampleOutput
{
    public string EnvExampleFile { get; set; }          // Generated example file
    public string[] EnvFile { get; set; }               // Source files
    public string ExampleFilepath { get; set; }         // Full path to example
    public List<string> AddedKeys { get; set; }         // Keys added to example
    public Dictionary<string, string> Injected { get; set; }  // New values
    public Dictionary<string, string> PreExisted { get; set; } // Existing values
}
```

## Service Interfaces

### IDotEnvService

Service interface for dependency injection.

```csharp
public interface IDotEnvService
{
    string? Get(string key);
    void Set(string key, string value, bool encrypt = false);
    Dictionary<string, string> GetAll();
    void Reload();
}
```

**Registration:**
```csharp
// In Program.cs or Startup.cs
builder.Services.AddDotEnvX(options =>
{
    options.Path = new[] { ".env", ".env.local" };
    options.Overload = true;
});
```

**Usage:**
```csharp
public class MyService
{
    private readonly IDotEnvService _dotEnv;
    
    public MyService(IDotEnvService dotEnv)
    {
        _dotEnv = dotEnv;
    }
    
    public void DoWork()
    {
        var apiKey = _dotEnv.Get("API_KEY");
        _dotEnv.Set("PROCESSED", "true");
    }
}
```

## Dependency Injection

### ASP.NET Core Integration

**Configuration Builder Extension:**
```csharp
public static IConfigurationBuilder AddDotEnvX(
    this IConfigurationBuilder builder,
    Action<DotEnvOptions>? configureOptions = null)
```

**Example:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Add to configuration pipeline
builder.Configuration.AddDotEnvX(options =>
{
    options.Path = new[] { ".env", $".env.{builder.Environment.EnvironmentName}" };
    options.Overload = true;
});

// Access via IConfiguration
var apiUrl = builder.Configuration["API_URL"];
```

**Service Collection Extension:**
```csharp
public static IServiceCollection AddDotEnvX(
    this IServiceCollection services,
    Action<DotEnvOptions>? configureOptions = null)
```

**Example:**
```csharp
// Register services
builder.Services.AddDotEnvX(options =>
{
    options.Path = new[] { ".env" };
    options.Strict = false;
});

// Use in controllers
[ApiController]
public class WeatherController : ControllerBase
{
    private readonly IDotEnvService _dotEnv;
    
    public WeatherController(IDotEnvService dotEnv)
    {
        _dotEnv = dotEnv;
    }
    
    [HttpGet]
    public IActionResult Get()
    {
        var apiKey = _dotEnv.Get("WEATHER_API_KEY");
        // ...
    }
}
```

## Encryption

### ECIES Encryption

DotEnvX uses Elliptic Curve Integrated Encryption Scheme (ECIES) with:
- **Curve:** secp256k1
- **Key Derivation:** SHA-256
- **Cipher:** AES-256-GCM
- **Format:** Base64 encoded with "encrypted:" prefix

### Workflow

1. **Generate Keypair:**
```csharp
var keypair = DotEnv.GenerateKeypair();
```

2. **Save Keys:**
```csharp
// Private key - NEVER commit this
File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}");

// Public key - safe to commit
File.AppendAllText(".env", $"#DOTENV_PUBLIC_KEY={keypair.PublicKey}\n");
```

3. **Encrypt Values:**
```csharp
DotEnv.Set("API_SECRET", "super-secret", new SetOptions { Encrypt = true });
```

4. **Automatic Decryption:**
```csharp
DotEnv.Config(); // Automatically decrypts when .env.keys is present
var secret = Environment.GetEnvironmentVariable("API_SECRET"); // Decrypted value
```

## Error Handling

### Error Codes

Common error codes that can be caught or ignored:

- `MISSING_ENV_FILE` - Required .env file not found
- `PARSE_ERROR` - Invalid .env file syntax  
- `ENCRYPTION_ERROR` - Encryption/decryption failed
- `INVALID_KEY` - Invalid encryption key format
- `MISSING_KEY` - Required key not found

### Handling Strategies

**Strict Mode:**
```csharp
try
{
    var result = DotEnv.Config(new DotEnvOptions
    {
        Strict = true,
        Path = new[] { ".env.required" }
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Configuration failed: {ex.Message}");
    Environment.Exit(1);
}
```

**Ignore Specific Errors:**
```csharp
var result = DotEnv.Config(new DotEnvOptions
{
    Ignore = new[] { "MISSING_ENV_FILE" }
});
```

**Check Result:**
```csharp
var result = DotEnv.Config();
if (result.Error != null)
{
    Logger.LogWarning($"Environment loading had issues: {result.Error.Message}");
}
```

## Best Practices

### 1. Security

```gitignore
# .gitignore
.env
.env.local
.env.*.local
.env.keys
*.env.keys
```

Never commit:
- `.env` files with real secrets
- `.env.keys` files
- Any file containing private keys

### 2. File Organization

```
project/
├── .env                 # Default environment (committed with encrypted values)
├── .env.example         # Example file (committed)
├── .env.keys           # Private keys (NEVER commit)
├── .env.local          # Local overrides (not committed)
├── .env.development    # Development settings (committed)
├── .env.production     # Production settings (committed, encrypted)
└── .env.test          # Test settings (committed)
```

### 3. Loading Order

```csharp
DotEnv.Config(new DotEnvOptions
{
    Path = new[]
    {
        ".env",                        // 1. Base configuration
        $".env.{environment}",         // 2. Environment-specific
        ".env.local",                  // 3. Local overrides
        $".env.{environment}.local"    // 4. Local environment-specific
    },
    Overload = true // Later files override earlier ones
});
```

### 4. Variable Expansion

```env
# .env
BASE_URL=https://api.example.com
API_V1=${BASE_URL}/v1
USER_ENDPOINT=${API_V1}/users
```

### 5. Performance

```csharp
// Good - load once at startup
public class Startup
{
    public Startup()
    {
        DotEnv.Config();
    }
}

// Good - use caching service
public class CachedEnvService
{
    private readonly Dictionary<string, string> _cache;
    
    public CachedEnvService()
    {
        var result = DotEnv.Config();
        _cache = result.Parsed ?? new Dictionary<string, string>();
    }
    
    public string? Get(string key) => _cache.TryGetValue(key, out var value) ? value : null;
}
```

### 6. Testing

```csharp
// Test setup
[SetUp]
public void Setup()
{
    // Use test-specific .env
    DotEnv.Config(new DotEnvOptions
    {
        Path = new[] { ".env.test" },
        Overload = true
    });
}

// Mock environment in tests
var testEnv = new Dictionary<string, string>
{
    ["API_KEY"] = "test-key",
    ["DATABASE_URL"] = "sqlite::memory:"
};

DotEnv.Config(new DotEnvOptions
{
    ProcessEnv = testEnv
});
```

## Thread Safety

- **Static Methods:** Thread-safe for read operations
- **File Operations:** Use locking for concurrent writes
- **Encryption/Decryption:** Thread-safe
- **Service Instances:** Thread-safe when registered as singleton

## Migration Guide

### From dotenv.net

```csharp
// Old (dotenv.net)
DotNetEnv.Env.Load();
var value = DotNetEnv.Env.GetString("KEY");

// New (DotEnvX)
DotEnv.Config();
var value = Environment.GetEnvironmentVariable("KEY");
```

### From Microsoft.Extensions.Configuration

```csharp
// Can use together
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddDotEnvX(options => 
    {
        options.Path = new[] { ".env" };
    })
    .AddEnvironmentVariables();
```

## Version Compatibility

| DotEnvX Version | .NET Version | Status |
|-----------------|--------------|--------|
| 1.0.0+          | .NET 8.0+    | ✅ Fully Supported |
| 1.0.0+          | .NET 6.0-7.0 | ⚠️ Should work (untested) |
| 1.0.0+          | .NET 5.0     | ❌ Not Supported |
| 1.0.0+          | .NET Framework | ❌ Not Supported |