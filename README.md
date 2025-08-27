# DotEnvX for .NET

[![NuGet](https://img.shields.io/nuget/v/DotEnvX.svg)](https://www.nuget.org/packages/DotEnvX/)
[![License](https://img.shields.io/badge/license-BSD--3--Clause-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)](https://dotnet.microsoft.com)

A secure, feature-complete port of [dotenvx](https://github.com/dotenvx/dotenvx) for modern .NET applications. Load environment variables from `.env` files with support for encryption, multiple environments, and variable expansion.

## ✨ Features

- 🔐 **Built-in Encryption** - Secure your secrets with ECIES encryption
- 📁 **Multiple File Support** - Load multiple `.env` files with precedence
- 🔄 **Variable Expansion** - Reference other variables with `${VAR}` syntax
- 💉 **Dependency Injection** - First-class support for ASP.NET Core DI
- 🎯 **Type Safety** - Full C# type safety with nullable reference types
- 🌍 **Cross-Platform** - Works on Windows, Linux, and macOS
- 📝 **Multi-line Values** - Support for multi-line strings
- 🚀 **Zero Dependencies** - Minimal dependencies for core functionality

## 📦 Installation

```bash
# Core library
dotnet add package DotEnvX

# ASP.NET Core integration
dotnet add package DotEnvX.Extensions.DependencyInjection
```

## 🚀 Quick Start

### Basic Usage

Create a `.env` file in your project root:
```env
DATABASE_URL=postgresql://localhost/mydb
API_KEY=sk-1234567890abcdef
DEBUG=true
PORT=3000
```

Load it in your application:
```csharp
using DotEnvX.Core;

// Load .env file
DotEnv.Config();

// Access variables
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
Console.WriteLine($"Database: {dbUrl}");
```

### ASP.NET Core Integration

```csharp
using DotEnvX.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add DotEnvX to the configuration pipeline
builder.Configuration.AddDotEnvX();

// Or with options
builder.Services.AddDotEnvX(options =>
{
    options.Path = new[] { ".env", $".env.{builder.Environment.EnvironmentName}" };
    options.Overload = true;
});

var app = builder.Build();
```

### Encryption

Protect sensitive values with built-in encryption:

```csharp
// Generate a keypair
var keypair = DotEnv.GenerateKeypair();

// Save your keys
File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}");
File.AppendAllText(".env", $"#DOTENV_PUBLIC_KEY={keypair.PublicKey}\n");

// Encrypt a value
DotEnv.Set("API_SECRET", "super-secret-value", new SetOptions
{
    Path = new[] { ".env" },
    Encrypt = true
});
```

Your `.env` file will contain:
```env
#DOTENV_PUBLIC_KEY=04abc123...
API_SECRET="encrypted:BDb7t3QkTRp2..."
```

Values are automatically decrypted when loaded:
```csharp
DotEnv.Config(); // Automatically finds and uses .env.keys
var secret = Environment.GetEnvironmentVariable("API_SECRET");
// secret = "super-secret-value" (decrypted)
```

## 📚 Documentation

### Configuration Options

```csharp
DotEnv.Config(new DotEnvOptions
{
    Path = new[] { ".env", ".env.local" },    // Files to load
    Overload = true,                          // Override existing vars
    Strict = true,                             // Throw on missing files
    Ignore = new[] { "MISSING_ENV_FILE" },    // Ignore specific errors
    Encoding = "utf-8",                        // File encoding
    Debug = true,                              // Enable debug logging
    Convention = "nextjs"                      // Use framework convention
});
```

### Variable Expansion

Reference other variables in your `.env` file:
```env
BASE_URL=https://api.example.com
API_V1=${BASE_URL}/v1
USER_ENDPOINT=${API_V1}/users
```

### Multiple Environments

Load environment-specific configurations:
```csharp
var env = builder.Environment.EnvironmentName;

DotEnv.Config(new DotEnvOptions
{
    Path = new[]
    {
        ".env",                    // Shared
        $".env.{env}",             // Environment-specific
        ".env.local",              // Local overrides
        $".env.{env}.local"        // Local environment overrides
    },
    Overload = true
});
```

### Dependency Injection

Use the `IDotEnvService` in your services:
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

### Parse Without Loading

Parse `.env` content without affecting environment:
```csharp
var content = File.ReadAllText(".env");
var values = DotEnv.Parse(content);

foreach (var kvp in values)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

### Generate Example Files

Create `.env.example` with safe defaults:
```csharp
var result = DotEnv.GenExample(Directory.GetCurrentDirectory(), ".env");
// Creates .env.example with placeholder values
```

## 🔒 Security Best Practices

1. **Never commit secrets to version control**
   ```gitignore
   .env
   .env.local
   .env.keys
   .env.*.local
   ```

2. **Use encryption for sensitive values**
   ```csharp
   DotEnv.Set("API_KEY", secretValue, new SetOptions { Encrypt = true });
   ```

3. **Separate keys from encrypted values**
   - `.env` - Can be committed (contains encrypted values)
   - `.env.keys` - Never commit (contains private keys)

4. **Use environment-specific files**
   - Development: `.env.development`
   - Production: Use vault files or environment variables

## 🏗️ Production Deployment

### Using Vault Files

```csharp
// Set DOTENV_KEY environment variable
Environment.SetEnvironmentVariable("DOTENV_KEY", 
    "dotenv://:key_xxx@dotenvx.com/vault/.env.vault?environment=production");

// Automatically loads from vault
DotEnv.Config();
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .

# Don't include .env.keys in production
RUN rm -f .env.keys .env.local

ENV DOTENV_KEY=$DOTENV_KEY
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

### CI/CD

```yaml
# GitHub Actions
- name: Deploy
  env:
    DOTENV_KEY: ${{ secrets.DOTENV_KEY }}
  run: |
    dotnet build
    dotnet publish
```

## 📖 Examples

### Console Application
```csharp
using DotEnvX.Core;

DotEnv.Config();

var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var apiKey = Environment.GetEnvironmentVariable("API_KEY");

Console.WriteLine($"Connecting to: {dbUrl}");
Console.WriteLine($"API Key: {apiKey?[..10]}...");
```

### Web API
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvX(options =>
{
    options.Path = new[] { ".env", $".env.{builder.Environment.EnvironmentName}" };
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapGet("/", () => new
{
    Environment = app.Environment.EnvironmentName,
    Database = Environment.GetEnvironmentVariable("DATABASE_URL")
});

app.Run();
```

### Worker Service
```csharp
public class Worker : BackgroundService
{
    private readonly IDotEnvService _dotEnv;
    
    public Worker(IDotEnvService dotEnv)
    {
        _dotEnv = dotEnv;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = int.Parse(_dotEnv.Get("POLL_INTERVAL") ?? "5000");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            // Do work
            await Task.Delay(interval, stoppingToken);
        }
    }
}
```

## 🧪 Testing

```bash
# Run tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the BSD 3-Clause License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Original [dotenvx](https://github.com/dotenvx/dotenvx) by [@motdotla](https://github.com/motdotla)
- Built with [BouncyCastle](https://www.bouncycastle.org/) for cryptography
- Inspired by the Node.js ecosystem

## 📊 Status

- ✅ Core functionality
- ✅ Encryption/Decryption
- ✅ Variable expansion
- ✅ Multiple file support
- ✅ ASP.NET Core integration
- ✅ Dependency injection
- ✅ Configuration provider
- ✅ Example generator
- ✅ Vault file support

## 🔗 Links

- [NuGet Package](https://www.nuget.org/packages/DotEnvX/)
- [Documentation](https://github.com/plsft/DotEnvX/wiki)
- [Original dotenvx](https://github.com/dotenvx/dotenvx)
- [Report Issues](https://github.com/plsft/DotEnvX/issues)

---

Made with ❤️ for the .NET community