# DotEnvX API Documentation

## Table of Contents

- [DotEnv Static Class](#dotenv-static-class)
- [Options Classes](#options-classes)
- [Service Interfaces](#service-interfaces)
- [Models](#models)
- [Encryption](#encryption)
- [Dependency Injection](#dependency-injection)

## DotEnv Static Class

The main entry point for the DotEnvX library.

### Config

Loads environment variables from .env files.

```csharp
public static DotEnvConfigResult Config(DotEnvOptions? options = null)
```

**Parameters:**
- `options` - Configuration options (optional)

**Returns:** `DotEnvConfigResult` containing loaded and parsed variables

**Example:**
```csharp
var result = DotEnv.Config(new DotEnvOptions
{
    Path = new[] { ".env", ".env.production" },
    Overload = true,
    Strict = false
});
```

### Parse

Parses .env content into a dictionary.

```csharp
public static Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null)
```

**Parameters:**
- `src` - The .env file content to parse
- `options` - Parse options (optional)

**Returns:** Dictionary of parsed key-value pairs

**Example:**
```csharp
var content = File.ReadAllText(".env");
var vars = DotEnv.Parse(content, new DotEnvParseOptions
{
    ProcessEnvVars = Environment.GetEnvironmentVariables()
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
var output = DotEnv.Set("API_KEY", "secret123", new SetOptions
{
    Path = new[] { ".env" },
    Encrypt = true,
    Force = false
});
```

### Get

Retrieves an environment variable value.

```csharp
public static string? Get(string key, GetOptions? options = null)
```

**Parameters:**
- `key` - Variable name
- `options` - Get options (optional)

**Returns:** Variable value or null if not found

**Example:**
```csharp
var apiKey = DotEnv.Get("API_KEY", new GetOptions
{
    Path = new[] { ".env", ".env.local" }
});
```

### GenerateKeypair

Generates an encryption keypair.

```csharp
public static Keypair GenerateKeypair()
```

**Returns:** `Keypair` with public and private keys

**Example:**
```csharp
var keypair = DotEnv.GenerateKeypair();
File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}");
```

### Encrypt

Encrypts a value using a public key.

```csharp
public static string Encrypt(string value, string publicKey)
```

**Parameters:**
- `value` - Value to encrypt
- `publicKey` - Public key for encryption

**Returns:** Encrypted value string

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
- `encryptedValue` - Encrypted value (with or without prefix)
- `privateKey` - Private key for decryption

**Returns:** Decrypted value string

**Example:**
```csharp
var decrypted = DotEnv.Decrypt("encrypted:BDb7t3QkTRp2...", privateKey);
```

## Options Classes

### DotEnvOptions

Configuration options for loading .env files.

```csharp
public class DotEnvOptions
{
    public string[]? Path { get; set; }           // Files to load
    public bool Overload { get; set; }            // Override existing vars
    public bool Strict { get; set; }              // Throw on missing files
    public string[]? Ignore { get; set; }         // Error codes to ignore
    public string? EnvKeysFile { get; set; }      // Path to keys file
    public string? Convention { get; set; }       // Use convention
}
```

### DotEnvParseOptions

Options for parsing .env content.

```csharp
public class DotEnvParseOptions
{
    public IDictionary? ProcessEnvVars { get; set; }  // Vars for expansion
    public string? PrivateKey { get; set; }           // For decryption
}
```

### SetOptions

Options for setting variables.

```csharp
public class SetOptions
{
    public string[]? Path { get; set; }      // Target files
    public bool Encrypt { get; set; }        // Encrypt value
    public string? PublicKey { get; set; }   // Encryption key
    public bool Force { get; set; }          // Force overwrite
}
```

### GetOptions

Options for retrieving variables.

```csharp
public class GetOptions
{
    public string[]? Path { get; set; }      // Files to search
    public bool Decrypt { get; set; }        // Auto-decrypt
    public string? PrivateKey { get; set; }  // Decryption key
}
```

## Service Interfaces

### IDotEnvService

Service interface for dependency injection.

```csharp
public interface IDotEnvService
{
    DotEnvConfigResult Config(DotEnvOptions? options = null);
    Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null);
    SetOutput Set(string key, string value, SetOptions? options = null);
    string? Get(string key, GetOptions? options = null);
    Keypair GenerateKeypair();
    string Encrypt(string value, string publicKey);
    string Decrypt(string encryptedValue, string privateKey);
}
```

**Registration:**
```csharp
services.AddDotEnvX();
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
    }
}
```

## Models

### DotEnvConfigResult

Result of loading .env files.

```csharp
public class DotEnvConfigResult
{
    public Dictionary<string, string> Loaded { get; set; }   // All loaded vars
    public Dictionary<string, string> Parsed { get; set; }   // Newly parsed vars
    public List<string> Errors { get; set; }                 // Any errors
}
```

### Keypair

Encryption keypair.

```csharp
public class Keypair
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
    public bool Created { get; set; }       // File was created
    public bool Updated { get; set; }       // Value was updated
    public string? Error { get; set; }      // Error message
    public Dictionary<string, string> Changes { get; set; }  // Applied changes
}
```

## Encryption

### DotEnvEncryption

ECIES encryption implementation using secp256k1 curve.

```csharp
public static class DotEnvEncryption
{
    public static Keypair GenerateKeypair();
    public static string Encrypt(string plaintext, string publicKeyHex);
    public static string Decrypt(string ciphertext, string privateKeyHex);
}
```

**Algorithm Details:**
- **Curve:** secp256k1
- **KDF:** SHA-256
- **Cipher:** AES-256-GCM
- **Format:** Base64 encoded with "encrypted:" prefix

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

builder.Configuration.AddDotEnvX(options =>
{
    options.Path = new[] { ".env", $".env.{environment}" };
    options.Overload = true;
});
```

**Service Collection Extension:**
```csharp
public static IServiceCollection AddDotEnvX(
    this IServiceCollection services,
    Action<DotEnvOptions>? configureOptions = null)
```

**Example:**
```csharp
builder.Services.AddDotEnvX(options =>
{
    options.Path = new[] { ".env" };
    options.Strict = true;
});
```

## Error Handling

### Common Error Codes

- `MISSING_ENV_FILE` - Required .env file not found
- `PARSE_ERROR` - Invalid .env file syntax
- `ENCRYPTION_ERROR` - Encryption/decryption failed
- `INVALID_KEY` - Invalid encryption key format
- `MISSING_KEY` - Required key not found

**Example:**
```csharp
try
{
    var result = DotEnv.Config(new DotEnvOptions
    {
        Strict = true,
        Path = new[] { ".env.required" }
    });
}
catch (DotEnvException ex) when (ex.Code == "MISSING_ENV_FILE")
{
    Console.WriteLine($"Required file missing: {ex.Message}");
}
```

## Performance Considerations

### Best Practices

1. **Load Once:** Load .env files once at startup
2. **Cache Values:** Cache frequently accessed values
3. **Batch Operations:** Use bulk methods when possible
4. **Async Operations:** Use async methods for I/O operations

**Example:**
```csharp
// Good - load once
public class Startup
{
    public Startup()
    {
        DotEnv.Config();
    }
}

// Bad - loading repeatedly
public void ProcessRequest()
{
    DotEnv.Config(); // Don't do this
    var value = Environment.GetEnvironmentVariable("KEY");
}
```

## Thread Safety

- **Static Methods:** Thread-safe for read operations
- **File Operations:** Not thread-safe, use locking for concurrent writes
- **Encryption:** Thread-safe
- **Services:** Thread-safe when registered as singleton

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
// Old
builder.Configuration.AddJsonFile("appsettings.json");

// New (can use together)
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddDotEnvX();
```

## Version Compatibility

| DotEnvX Version | .NET Version | Status |
|-----------------|--------------|--------|
| 1.0.0+          | .NET 8.0+    | ✅ Supported |
| 1.0.0+          | .NET 6.0-7.0 | ⚠️ Untested |
| 1.0.0+          | .NET 5.0     | ❌ Not Supported |