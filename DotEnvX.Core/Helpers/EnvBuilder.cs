using DotEnvX.Core.Models;

namespace DotEnvX.Core.Helpers;

public static class EnvBuilder
{
    public static List<EnvConfig> BuildEnvs(DotEnvOptions options)
    {
        var envs = new List<EnvConfig>();
        
        // Handle convention-based loading (like nextjs)
        if (!string.IsNullOrEmpty(options.Convention))
        {
            var conventionPaths = GetConventionPaths(options.Convention);
            foreach (var path in conventionPaths)
            {
                envs.Add(new EnvConfig { Type = "envFile", Value = path });
            }
        }
        
        // Handle explicit paths
        if (options.Path != null && options.Path.Length > 0)
        {
            foreach (var path in options.Path)
            {
                // Check if it's a vault file
                if (path.EndsWith(".vault"))
                {
                    envs.Add(new EnvConfig { Type = "envVaultFile", Value = path });
                }
                else
                {
                    envs.Add(new EnvConfig { Type = "envFile", Value = path });
                }
            }
        }
        else if (string.IsNullOrEmpty(options.Convention))
        {
            // Default to .env file
            envs.Add(new EnvConfig { Type = "envFile", Value = ".env" });
        }
        
        // Add .env.vault if DOTENV_KEY is present
        if (!string.IsNullOrEmpty(options.DotEnvKey) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTENV_KEY")))
        {
            envs.Add(new EnvConfig { Type = "envVaultFile", Value = ".env.vault" });
        }
        
        return envs;
    }
    
    private static string[] GetConventionPaths(string convention)
    {
        return convention.ToLower() switch
        {
            "nextjs" => new[] 
            { 
                ".env",
                $".env.{GetNodeEnv()}",
                ".env.local",
                $".env.{GetNodeEnv()}.local"
            },
            "flow" => new[]
            {
                ".env",
                $".env.{GetFlowEnv()}"
            },
            _ => new[] { ".env" }
        };
    }
    
    private static string GetNodeEnv()
    {
        return Environment.GetEnvironmentVariable("NODE_ENV") ?? "development";
    }
    
    private static string GetFlowEnv()
    {
        return Environment.GetEnvironmentVariable("FLOW_ENV") ?? "development";
    }
}

public class EnvConfig
{
    public required string Type { get; set; }
    public required string Value { get; set; }
}