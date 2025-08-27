using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotEnvX.Core;
using DotEnvX.Core.Models;

namespace DotEnvX.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDotEnvX(this IServiceCollection services, Action<DotEnvOptions>? configureOptions = null)
    {
        var options = new DotEnvOptions();
        configureOptions?.Invoke(options);
        
        // Load environment variables immediately
        var result = DotEnv.Config(options);
        
        if (result.Error != null && options.Strict)
        {
            throw result.Error;
        }
        
        // Register options
        services.AddSingleton(options);
        
        // Register services
        services.AddSingleton<IDotEnvService, DotEnvService>();
        
        return services;
    }
    
    public static IServiceCollection AddDotEnvX(this IServiceCollection services, string path)
    {
        return services.AddDotEnvX(options =>
        {
            options.Path = new[] { path };
        });
    }
    
    public static IServiceCollection AddDotEnvX(this IServiceCollection services, params string[] paths)
    {
        return services.AddDotEnvX(options =>
        {
            options.Path = paths;
        });
    }
}

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddDotEnvX(this IConfigurationBuilder builder, Action<DotEnvOptions>? configureOptions = null)
    {
        var options = new DotEnvOptions();
        configureOptions?.Invoke(options);
        
        builder.Add(new DotEnvConfigurationSource { Options = options });
        return builder;
    }
    
    public static IConfigurationBuilder AddDotEnvX(this IConfigurationBuilder builder, string path)
    {
        return builder.AddDotEnvX(options =>
        {
            options.Path = new[] { path };
        });
    }
    
    public static IConfigurationBuilder AddDotEnvX(this IConfigurationBuilder builder, params string[] paths)
    {
        return builder.AddDotEnvX(options =>
        {
            options.Path = paths;
        });
    }
    
    public static IConfigurationBuilder AddEncryptedDotEnvX(this IConfigurationBuilder builder, string privateKey, params string[] paths)
    {
        return builder.AddDotEnvX(options =>
        {
            options.Path = paths;
            options.PrivateKey = privateKey;
        });
    }
}

// Service interface for DI
public interface IDotEnvService
{
    string? Get(string key);
    void Set(string key, string value, bool encrypt = true);
    Dictionary<string, string> GetAll();
    void Reload();
}

public class DotEnvService : IDotEnvService
{
    private readonly DotEnvOptions _options;
    private Dictionary<string, string> _cache;
    
    public DotEnvService(DotEnvOptions options)
    {
        _options = options;
        _cache = LoadEnvironment();
    }
    
    public string? Get(string key)
    {
        return _cache.TryGetValue(key, out var value) ? value : null;
    }
    
    public void Set(string key, string value, bool encrypt = true)
    {
        var setOptions = new SetOptions
        {
            Path = _options.Path,
            EnvKeysFile = _options.EnvKeysFile,
            Encrypt = encrypt
        };
        
        DotEnv.Set(key, value, setOptions);
        
        // Update cache
        _cache[key] = value;
        Environment.SetEnvironmentVariable(key, value);
    }
    
    public Dictionary<string, string> GetAll()
    {
        return new Dictionary<string, string>(_cache);
    }
    
    public void Reload()
    {
        _cache = LoadEnvironment();
    }
    
    private Dictionary<string, string> LoadEnvironment()
    {
        var result = DotEnv.Config(_options);
        return result.Parsed ?? new Dictionary<string, string>();
    }
}