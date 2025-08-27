using DotEnvX.Core;
using DotEnvX.Core.Models;
using DotEnvX.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DotEnvX.Samples;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("DotEnvX Samples");
        Console.WriteLine("================\n");
        
        // Create sample .env files
        CreateSampleEnvFiles();
        
        // Run samples
        BasicUsageSample();
        Console.WriteLine();
        
        AdvancedOptionsSample();
        Console.WriteLine();
        
        ParseSample();
        Console.WriteLine();
        
        EncryptionSample();
        Console.WriteLine();
        
        DependencyInjectionSample();
        Console.WriteLine();
        
        ConfigurationProviderSample();
        Console.WriteLine();
        
        VariableExpansionSample();
        Console.WriteLine();
        
        MultipleFilesSample();
        Console.WriteLine();
        
        GenerateExampleSample();
        
        Console.WriteLine("\n✅ All samples completed!");
    }
    
    static void CreateSampleEnvFiles()
    {
        // Create .env file
        File.WriteAllText(".env", @"# Main environment file
APP_NAME=DotEnvX Sample
APP_VERSION=1.0.0
DATABASE_URL=postgresql://localhost/sampledb
LOG_LEVEL=info
");
        
        // Create .env.local file
        File.WriteAllText(".env.local", @"# Local overrides
LOG_LEVEL=debug
LOCAL_SETTING=local_value
");
        
        // Create .env.production file
        File.WriteAllText(".env.production", @"# Production environment
DATABASE_URL=postgresql://prod-server/proddb
LOG_LEVEL=error
PRODUCTION_SETTING=prod_value
");
        
        Console.WriteLine("Created sample .env files");
    }
    
    static void BasicUsageSample()
    {
        Console.WriteLine("1. Basic Usage Sample");
        Console.WriteLine("---------------------");
        
        // Load .env file
        var result = DotEnv.Config();
        
        if (result.Error != null)
        {
            Console.WriteLine($"Error: {result.Error.Message}");
        }
        else
        {
            Console.WriteLine("✓ Loaded .env file");
            
            // Access variables
            var appName = Environment.GetEnvironmentVariable("APP_NAME");
            var appVersion = Environment.GetEnvironmentVariable("APP_VERSION");
            var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            
            Console.WriteLine($"  APP_NAME: {appName}");
            Console.WriteLine($"  APP_VERSION: {appVersion}");
            Console.WriteLine($"  DATABASE_URL: {dbUrl}");
        }
    }
    
    static void AdvancedOptionsSample()
    {
        Console.WriteLine("2. Advanced Options Sample");
        Console.WriteLine("--------------------------");
        
        var result = DotEnv.Config(new DotEnvOptions
        {
            Path = new[] { ".env", ".env.local" },
            Overload = true,
            Debug = false,
            Verbose = true
        });
        
        if (result.Parsed != null)
        {
            Console.WriteLine($"✓ Loaded {result.Parsed.Count} variables with overload");
            Console.WriteLine($"  LOG_LEVEL: {Environment.GetEnvironmentVariable("LOG_LEVEL")} (overridden from .env.local)");
            Console.WriteLine($"  LOCAL_SETTING: {Environment.GetEnvironmentVariable("LOCAL_SETTING")}");
        }
    }
    
    static void ParseSample()
    {
        Console.WriteLine("3. Parse Sample");
        Console.WriteLine("---------------");
        
        var envContent = @"
KEY1=value1
KEY2=""quoted value""
KEY3=123
# This is a comment
KEY4=value with spaces
";
        
        var parsed = DotEnv.Parse(envContent);
        
        Console.WriteLine($"✓ Parsed {parsed.Count} variables:");
        foreach (var kvp in parsed)
        {
            Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
        }
    }
    
    static void EncryptionSample()
    {
        Console.WriteLine("4. Encryption Sample");
        Console.WriteLine("--------------------");
        
        // Generate keypair
        var keypair = DotEnv.GenerateKeypair();
        Console.WriteLine("✓ Generated keypair");
        Console.WriteLine($"  Public Key: {keypair.PublicKey.Substring(0, 32)}...");
        Console.WriteLine($"  Private Key: {keypair.PrivateKey.Substring(0, 32)}...");
        
        // Encrypt a value
        var secretValue = "my-secret-api-key";
        var encrypted = DotEnv.Encrypt(secretValue, keypair.PublicKey);
        Console.WriteLine($"\n✓ Encrypted value:");
        Console.WriteLine($"  Original: {secretValue}");
        Console.WriteLine($"  Encrypted: {encrypted.Substring(0, 50)}...");
        
        // Decrypt the value
        var decrypted = DotEnv.Decrypt(encrypted, keypair.PrivateKey);
        Console.WriteLine($"\n✓ Decrypted value:");
        Console.WriteLine($"  Decrypted: {decrypted}");
        Console.WriteLine($"  Match: {decrypted == secretValue}");
        
        // Set encrypted value
        DotEnv.Set("SECRET_KEY", "another-secret", new SetOptions
        {
            Path = new[] { ".env.encrypted" },
            Encrypt = true
        });
        Console.WriteLine("\n✓ Created .env.encrypted with encrypted value");
    }
    
    static void DependencyInjectionSample()
    {
        Console.WriteLine("5. Dependency Injection Sample");
        Console.WriteLine("-------------------------------");
        
        var services = new ServiceCollection();
        
        // Add DotEnvX to DI
        services.AddDotEnvX(options =>
        {
            options.Path = new[] { ".env" };
        });
        
        // Build service provider
        var provider = services.BuildServiceProvider();
        
        // Get service
        var dotEnvService = provider.GetRequiredService<IDotEnvService>();
        
        Console.WriteLine("✓ Configured DI container");
        Console.WriteLine($"  APP_NAME from DI: {dotEnvService.Get("APP_NAME")}");
        
        // Set a new value
        dotEnvService.Set("NEW_KEY", "new_value_from_di", encrypt: false);
        Console.WriteLine($"  Set NEW_KEY: {dotEnvService.Get("NEW_KEY")}");
        
        // Get all variables
        var all = dotEnvService.GetAll();
        Console.WriteLine($"  Total variables: {all.Count}");
    }
    
    static void ConfigurationProviderSample()
    {
        Console.WriteLine("6. Configuration Provider Sample");
        Console.WriteLine("---------------------------------");
        
        var configuration = new ConfigurationBuilder()
            .AddDotEnvX(options =>
            {
                options.Path = new[] { ".env", ".env.local" };
                options.Overload = true;
            })
            .Build();
        
        Console.WriteLine("✓ Built IConfiguration with DotEnvX");
        Console.WriteLine($"  APP_NAME from config: {configuration["APP_NAME"]}");
        Console.WriteLine($"  LOG_LEVEL from config: {configuration["LOG_LEVEL"]}");
        
        // Bind to strongly-typed options
        var appSettings = new AppSettings();
        configuration.Bind(appSettings);
        Console.WriteLine($"  Bound to AppSettings: {appSettings.APP_NAME} v{appSettings.APP_VERSION}");
    }
    
    static void VariableExpansionSample()
    {
        Console.WriteLine("7. Variable Expansion Sample");
        Console.WriteLine("-----------------------------");
        
        // Create file with variable expansion
        File.WriteAllText(".env.expansion", @"
BASE_URL=https://api.example.com
API_V1=${BASE_URL}/v1
API_V2=${BASE_URL}/v2
USER_ENDPOINT=${API_V1}/users
");
        
        DotEnv.Config(new DotEnvOptions { Path = new[] { ".env.expansion" } });
        
        Console.WriteLine("✓ Loaded with variable expansion:");
        Console.WriteLine($"  BASE_URL: {Environment.GetEnvironmentVariable("BASE_URL")}");
        Console.WriteLine($"  API_V1: {Environment.GetEnvironmentVariable("API_V1")}");
        Console.WriteLine($"  USER_ENDPOINT: {Environment.GetEnvironmentVariable("USER_ENDPOINT")}");
    }
    
    static void MultipleFilesSample()
    {
        Console.WriteLine("8. Multiple Files Sample");
        Console.WriteLine("-------------------------");
        
        // Simulate different environments
        var environment = "production";
        
        var paths = environment == "production" 
            ? new[] { ".env", ".env.production" }
            : new[] { ".env", ".env.local" };
        
        DotEnv.Config(new DotEnvOptions
        {
            Path = paths,
            Overload = true
        });
        
        Console.WriteLine($"✓ Loaded for {environment} environment:");
        Console.WriteLine($"  DATABASE_URL: {Environment.GetEnvironmentVariable("DATABASE_URL")}");
        Console.WriteLine($"  LOG_LEVEL: {Environment.GetEnvironmentVariable("LOG_LEVEL")}");
        
        if (environment == "production")
        {
            Console.WriteLine($"  PRODUCTION_SETTING: {Environment.GetEnvironmentVariable("PRODUCTION_SETTING")}");
        }
    }
    
    static void GenerateExampleSample()
    {
        Console.WriteLine("9. Generate Example Sample");
        Console.WriteLine("---------------------------");
        
        // Create a file with secrets
        File.WriteAllText(".env.with_secrets", @"
DATABASE_URL=postgresql://user:pass@localhost/db
API_KEY=sk-1234567890abcdef
API_SECRET=secret_value_here
PUBLIC_URL=https://example.com
DEBUG=true
");
        
        var result = DotEnv.GenExample(Directory.GetCurrentDirectory(), ".env.with_secrets");
        
        Console.WriteLine("✓ Generated .env.with_secrets.example");
        Console.WriteLine($"  Added keys: {string.Join(", ", result.AddedKeys)}");
        
        // Show the generated example
        var exampleContent = File.ReadAllText(result.ExampleFilepath);
        Console.WriteLine("\n  Example file content:");
        foreach (var line in exampleContent.Split('\n').Take(10))
        {
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
            {
                Console.WriteLine($"    {line}");
            }
        }
    }
}

// Sample settings class
public class AppSettings
{
    public string? APP_NAME { get; set; }
    public string? APP_VERSION { get; set; }
    public string? DATABASE_URL { get; set; }
    public string? LOG_LEVEL { get; set; }
}
