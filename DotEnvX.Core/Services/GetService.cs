using DotEnvX.Core.Models;
using DotEnvX.Core.Helpers;

namespace DotEnvX.Core.Services;

public class GetService
{
    private readonly string _key;
    private readonly GetOptions _options;
    
    public GetService(string key, GetOptions options)
    {
        _key = key;
        _options = options;
    }
    
    public string? Run()
    {
        try
        {
            // First check current environment
            var currentValue = Environment.GetEnvironmentVariable(_key);
            if (!string.IsNullOrEmpty(currentValue) && !_options.Overload)
            {
                return currentValue;
            }
            
            // Build and run config to load env files
            var configOptions = new DotEnvOptions
            {
                Overload = _options.Overload,
                Ignore = _options.Ignore,
                EnvKeysFile = _options.EnvKeysFile,
                Strict = _options.Strict
            };
            
            var result = DotEnv.Config(configOptions);
            
            if (_options.Strict && result.Error != null)
            {
                throw result.Error;
            }
            
            // Try to get the value from parsed results
            if (result.Parsed?.TryGetValue(_key, out var value) == true)
            {
                return value;
            }
            
            // Check environment again (in case it was set during config)
            return Environment.GetEnvironmentVariable(_key);
        }
        catch (Exception ex)
        {
            if (_options.Strict)
            {
                throw new DotEnvError(
                    ErrorCodes.KEY_NOT_FOUND,
                    $"Key '{_key}' not found",
                    ex.Message
                );
            }
            
            return null;
        }
    }
}