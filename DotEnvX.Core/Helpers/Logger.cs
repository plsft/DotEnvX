using DotEnvX.Core.Models;

namespace DotEnvX.Core.Helpers;

public class Logger
{
    private LogLevel _logLevel = LogLevel.Info;
    private bool _quiet;
    private bool _verbose;
    private bool _debug;
    
    public void SetLogLevel(LogLevel level) => _logLevel = level;
    public void SetQuiet(bool quiet) => _quiet = quiet;
    public void SetVerbose(bool verbose) => _verbose = verbose;
    public void SetDebug(bool debug) => _debug = debug;
    
    public void Error(string message)
    {
        if (!_quiet && _logLevel >= LogLevel.Error)
        {
            Console.Error.WriteLine($"[ERROR] {message}");
        }
    }
    
    public void Warn(string message)
    {
        if (!_quiet && _logLevel >= LogLevel.Warn)
        {
            Console.WriteLine($"[WARN] {message}");
        }
    }
    
    public void Success(string message)
    {
        if (!_quiet && _logLevel >= LogLevel.Success)
        {
            Console.WriteLine($"[SUCCESS] {message}");
        }
    }
    
    public void Info(string message)
    {
        if (!_quiet && _logLevel >= LogLevel.Info)
        {
            Console.WriteLine(message);
        }
    }
    
    public void Verbose(string message)
    {
        if (_verbose || _logLevel >= LogLevel.Verbose)
        {
            Console.WriteLine($"[VERBOSE] {message}");
        }
    }
    
    public void Debug(string message)
    {
        if (_debug || _logLevel >= LogLevel.Debug)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }
    }
}