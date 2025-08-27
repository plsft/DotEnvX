using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace DotEnvX.Core.Services;

public class LsService
{
    private readonly string _directory;
    private readonly string[] _patterns;
    private readonly string[] _excludePatterns;
    
    public LsService(string directory, string[] patterns, string[] excludePatterns)
    {
        _directory = directory;
        _patterns = patterns;
        _excludePatterns = excludePatterns;
    }
    
    public string[] Run()
    {
        var matcher = new Matcher();
        
        // Add include patterns
        foreach (var pattern in _patterns)
        {
            matcher.AddInclude(pattern);
        }
        
        // Add exclude patterns
        foreach (var excludePattern in _excludePatterns)
        {
            matcher.AddExclude(excludePattern);
        }
        
        // Execute the matcher
        var directoryInfo = new DirectoryInfo(_directory);
        var result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));
        
        // Return matched file paths
        return result.Files
            .Select(f => Path.Combine(_directory, f.Path))
            .OrderBy(f => f)
            .ToArray();
    }
}