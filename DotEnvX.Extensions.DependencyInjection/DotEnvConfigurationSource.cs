using Microsoft.Extensions.Configuration;
using DotEnvX.Core.Models;

namespace DotEnvX.Extensions.DependencyInjection;

public class DotEnvConfigurationSource : IConfigurationSource
{
    public DotEnvOptions? Options { get; set; }
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DotEnvConfigurationProvider(Options);
    }
}

public class DotEnvConfigurationProvider : ConfigurationProvider
{
    private readonly DotEnvOptions? _options;
    
    public DotEnvConfigurationProvider(DotEnvOptions? options)
    {
        _options = options;
    }
    
    public override void Load()
    {
        var result = Core.DotEnv.Config(_options);
        
        if (result.Error != null && _options?.Strict == true)
        {
            throw result.Error;
        }
        
        if (result.Parsed != null)
        {
            Data = new Dictionary<string, string?>(result.Parsed!);
        }
    }
}