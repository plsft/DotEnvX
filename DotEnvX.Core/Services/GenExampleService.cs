using System.Text;
using DotEnvX.Core.Models;
using DotEnvX.Core.Parser;

namespace DotEnvX.Core.Services;

public class GenExampleService
{
    private readonly string _directory;
    private readonly string _envFile;
    
    public GenExampleService(string directory, string envFile)
    {
        _directory = directory;
        _envFile = envFile;
    }
    
    public GenExampleOutput Run()
    {
        var envFilePath = Path.Combine(_directory, _envFile);
        var exampleFileName = _envFile + ".example";
        var exampleFilePath = Path.Combine(_directory, exampleFileName);
        
        var addedKeys = new List<string>();
        var injected = new Dictionary<string, string>();
        var preExisted = new Dictionary<string, string>();
        
        // Read the source env file
        if (!File.Exists(envFilePath))
        {
            throw new FileNotFoundException($"Environment file not found: {envFilePath}");
        }
        
        var envContent = File.ReadAllText(envFilePath);
        var envVars = DotEnvParser.Parse(envContent, new DotEnvParseOptions());
        
        // Read existing example file if it exists
        Dictionary<string, string> existingExample = new();
        if (File.Exists(exampleFilePath))
        {
            var exampleContent = File.ReadAllText(exampleFilePath);
            existingExample = DotEnvParser.Parse(exampleContent, new DotEnvParseOptions());
        }
        
        // Build example content
        var exampleBuilder = new StringBuilder();
        
        // Add header comment
        exampleBuilder.AppendLine("# Example environment file");
        exampleBuilder.AppendLine($"# Copy this file to {_envFile} and fill in your values");
        exampleBuilder.AppendLine();
        
        foreach (var kvp in envVars)
        {
            var key = kvp.Key;
            var exampleValue = "";
            
            // Determine example value
            if (IsSecretValue(key, kvp.Value))
            {
                exampleValue = GenerateExampleValue(key);
            }
            else if (IsBooleanValue(kvp.Value))
            {
                exampleValue = "false";
            }
            else if (IsNumericValue(kvp.Value))
            {
                exampleValue = "0";
            }
            else if (IsUrlValue(kvp.Value))
            {
                exampleValue = "https://example.com";
            }
            else if (IsEmailValue(kvp.Value))
            {
                exampleValue = "user@example.com";
            }
            else if (IsPathValue(kvp.Value))
            {
                exampleValue = "/path/to/file";
            }
            else
            {
                // Use the actual value if it's not sensitive
                exampleValue = kvp.Value;
            }
            
            // Track changes
            if (existingExample.ContainsKey(key))
            {
                preExisted[key] = existingExample[key];
            }
            else
            {
                addedKeys.Add(key);
                injected[key] = exampleValue;
            }
            
            // Add to example file
            exampleBuilder.AppendLine($"{key}={exampleValue}");
        }
        
        // Write example file
        File.WriteAllText(exampleFilePath, exampleBuilder.ToString());
        
        return new GenExampleOutput
        {
            EnvExampleFile = exampleFileName,
            EnvFile = new[] { _envFile },
            ExampleFilepath = exampleFilePath,
            AddedKeys = addedKeys,
            Injected = injected,
            PreExisted = preExisted
        };
    }
    
    private static bool IsSecretValue(string key, string value)
    {
        var secretKeywords = new[] 
        { 
            "SECRET", "PASSWORD", "PWD", "TOKEN", "KEY", "API", "PRIVATE",
            "CREDENTIAL", "AUTH", "CERTIFICATE", "CERT"
        };
        
        var upperKey = key.ToUpper();
        return secretKeywords.Any(keyword => upperKey.Contains(keyword)) ||
               value.StartsWith("encrypted:");
    }
    
    private static string GenerateExampleValue(string key)
    {
        var upperKey = key.ToUpper();
        
        if (upperKey.Contains("PASSWORD") || upperKey.Contains("PWD"))
            return "your_password_here";
        if (upperKey.Contains("SECRET"))
            return "your_secret_here";
        if (upperKey.Contains("TOKEN"))
            return "your_token_here";
        if (upperKey.Contains("API") && upperKey.Contains("KEY"))
            return "your_api_key_here";
        if (upperKey.Contains("KEY"))
            return "your_key_here";
        if (upperKey.Contains("AUTH"))
            return "your_auth_token_here";
        
        return "your_value_here";
    }
    
    private static bool IsBooleanValue(string value)
    {
        var lower = value.ToLower();
        return lower == "true" || lower == "false" || lower == "yes" || lower == "no" ||
               lower == "1" || lower == "0" || lower == "on" || lower == "off";
    }
    
    private static bool IsNumericValue(string value)
    {
        return int.TryParse(value, out _) || double.TryParse(value, out _);
    }
    
    private static bool IsUrlValue(string value)
    {
        return value.StartsWith("http://") || value.StartsWith("https://") ||
               value.StartsWith("ftp://") || value.StartsWith("ssh://");
    }
    
    private static bool IsEmailValue(string value)
    {
        return value.Contains("@") && value.Contains(".");
    }
    
    private static bool IsPathValue(string value)
    {
        return value.StartsWith("/") || value.StartsWith("./") || value.StartsWith("../") ||
               value.Contains("\\") || value.Contains(":");
    }
}