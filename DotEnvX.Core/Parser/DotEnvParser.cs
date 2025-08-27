using System.Text;
using System.Text.RegularExpressions;
using DotEnvX.Core.Models;
using DotEnvX.Core.Encryption;

namespace DotEnvX.Core.Parser;

public static class DotEnvParser
{
    private static readonly Regex LineRegex = new(@"^\s*(?:export\s+)?([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)?\s*$", 
        RegexOptions.Multiline);
    
    private static readonly Regex ExpandVarRegex = new(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}|\$([A-Za-z_][A-Za-z0-9_]*)", 
        RegexOptions.Compiled);

    public static Dictionary<string, string> Parse(string src, DotEnvParseOptions? options = null)
    {
        return Parse(Encoding.UTF8.GetBytes(src), options);
    }

    public static Dictionary<string, string> Parse(byte[] src, DotEnvParseOptions? options = null)
    {
        options ??= new DotEnvParseOptions();
        var processEnv = options.ProcessEnv ?? new Dictionary<string, string>();
        
        var content = Encoding.UTF8.GetString(src);
        var result = new Dictionary<string, string>();
        
        // Remove BOM if present
        if (content.StartsWith("\uFEFF"))
        {
            content = content.Substring(1);
        }
        
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var currentKey = string.Empty;
        var currentValue = new StringBuilder();
        var isMultiline = false;
        var quoteChar = '\0';
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Skip comments and empty lines (unless in multiline)
            if (!isMultiline)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                    continue;
            }
            
            if (isMultiline)
            {
                // Continue building multiline value
                if (quoteChar != '\0')
                {
                    var endQuoteIndex = FindClosingQuote(line, quoteChar);
                    if (endQuoteIndex >= 0)
                    {
                        currentValue.Append(line.Substring(0, endQuoteIndex));
                        var value = ProcessValue(currentValue.ToString(), quoteChar, processEnv);
                        
                        // Check for encryption
                        if (value.StartsWith("encrypted:") && !string.IsNullOrEmpty(options.PrivateKey))
                        {
                            value = DecryptValue(value, options.PrivateKey);
                        }
                        
                        AddToResult(result, currentKey, value, options);
                        
                        isMultiline = false;
                        currentKey = string.Empty;
                        currentValue.Clear();
                        quoteChar = '\0';
                    }
                    else
                    {
                        currentValue.AppendLine(line);
                    }
                }
                continue;
            }
            
            var match = LineRegex.Match(line);
            if (match.Success)
            {
                currentKey = match.Groups[1].Value;
                var rawValue = match.Groups[2].Value.Trim();
                
                if (string.IsNullOrEmpty(rawValue))
                {
                    AddToResult(result, currentKey, string.Empty, options);
                    continue;
                }
                
                // Check for quotes
                if (rawValue.StartsWith('"') || rawValue.StartsWith('\''))
                {
                    quoteChar = rawValue[0];
                    var endQuoteIndex = FindClosingQuote(rawValue.Substring(1), quoteChar);
                    
                    if (endQuoteIndex >= 0)
                    {
                        // Single line quoted value
                        var value = ProcessValue(rawValue.Substring(1, endQuoteIndex), quoteChar, processEnv);
                        
                        // Check for encryption
                        if (value.StartsWith("encrypted:") && !string.IsNullOrEmpty(options.PrivateKey))
                        {
                            value = DecryptValue(value, options.PrivateKey);
                        }
                        
                        AddToResult(result, currentKey, value, options);
                    }
                    else
                    {
                        // Start of multiline value
                        isMultiline = true;
                        currentValue.Append(rawValue.Substring(1));
                    }
                }
                else
                {
                    // Unquoted value
                    var value = ProcessValue(rawValue, '\0', processEnv);
                    
                    // Check for encryption
                    if (value.StartsWith("encrypted:") && !string.IsNullOrEmpty(options.PrivateKey))
                    {
                        value = DecryptValue(value, options.PrivateKey);
                    }
                    
                    AddToResult(result, currentKey, value, options);
                }
            }
        }
        
        return result;
    }
    
    private static void AddToResult(Dictionary<string, string> result, string key, string value, DotEnvParseOptions options)
    {
        if (options.Override || options.Overload || !result.ContainsKey(key))
        {
            result[key] = value;
        }
    }
    
    private static int FindClosingQuote(string str, char quoteChar)
    {
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == quoteChar && (i == 0 || str[i - 1] != '\\'))
            {
                return i;
            }
        }
        return -1;
    }
    
    private static string ProcessValue(string value, char quoteChar, IDictionary<string, string> processEnv)
    {
        // Remove trailing comment if unquoted
        if (quoteChar == '\0')
        {
            var commentIndex = value.IndexOf('#');
            if (commentIndex > 0 && value[commentIndex - 1] == ' ')
            {
                value = value.Substring(0, commentIndex).TrimEnd();
            }
        }
        
        // Expand variables
        value = ExpandVariables(value, processEnv);
        
        // Process escape sequences for double quotes
        if (quoteChar == '"')
        {
            value = UnescapeValue(value);
        }
        
        return value;
    }
    
    private static string ExpandVariables(string value, IDictionary<string, string> processEnv)
    {
        return ExpandVarRegex.Replace(value, match =>
        {
            var varName = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (processEnv.TryGetValue(varName, out var varValue))
            {
                return varValue;
            }
            if (Environment.GetEnvironmentVariable(varName) is { } envValue)
            {
                return envValue;
            }
            return match.Value; // Keep original if not found
        });
    }
    
    private static string UnescapeValue(string value)
    {
        return value
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\'", "'")
            .Replace("\\\\", "\\");
    }
    
    private static string DecryptValue(string value, string privateKey)
    {
        if (!value.StartsWith("encrypted:"))
            return value;
            
        var encryptedPart = value.Substring("encrypted:".Length);
        return DotEnvEncryption.Decrypt(encryptedPart, privateKey);
    }
}