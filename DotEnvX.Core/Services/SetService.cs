using System.Text;
using System.Text.RegularExpressions;
using DotEnvX.Core.Models;
using DotEnvX.Core.Parser;
using DotEnvX.Core.Encryption;

namespace DotEnvX.Core.Services;

public class SetService
{
    private readonly string _key;
    private readonly string _value;
    private readonly SetOptions _options;
    
    public SetService(string key, string value, SetOptions options)
    {
        _key = key;
        _value = value;
        _options = options;
    }
    
    public SetOutput Run()
    {
        var processedEnvs = new List<SetProcessedEnv>();
        var changedFilepaths = new List<string>();
        var unchangedFilepaths = new List<string>();
        
        var paths = _options.Path ?? new[] { ".env" };
        
        foreach (var path in paths)
        {
            var result = SetInFile(path);
            processedEnvs.Add(result);
            
            if (result.Changed)
            {
                changedFilepaths.Add(result.Filepath);
            }
            else
            {
                unchangedFilepaths.Add(result.Filepath);
            }
        }
        
        return new SetOutput
        {
            ProcessedEnvs = processedEnvs,
            ChangedFilepaths = changedFilepaths,
            UnchangedFilepaths = unchangedFilepaths
        };
    }
    
    private SetProcessedEnv SetInFile(string filepath)
    {
        var fullPath = Path.GetFullPath(filepath);
        var result = new SetProcessedEnv
        {
            Key = _key,
            Value = _value,
            Filepath = fullPath,
            EnvFilepath = filepath,
            EnvSrc = "",
            Changed = false
        };
        
        try
        {
            // Read existing file or create new content
            string content;
            if (File.Exists(fullPath))
            {
                content = File.ReadAllText(fullPath);
            }
            else
            {
                content = "";
            }
            
            result.EnvSrc = content;
            
            // Check if we need to encrypt
            if (_options.Encrypt)
            {
                // Find or generate keys
                var (publicKey, privateKey, privateKeyName) = GetOrGenerateKeys(filepath);
                result.PublicKey = publicKey;
                result.PrivateKey = privateKey;
                result.PrivateKeyName = privateKeyName;
                
                // Encrypt the value
                var encryptedValue = DotEnvEncryption.Encrypt(_value, publicKey);
                result.EncryptedValue = encryptedValue;
                
                // Update content with encrypted value
                content = UpdateEnvContent(content, _key, encryptedValue);
                
                // Save private key if newly generated
                if (!string.IsNullOrEmpty(privateKey) && !KeyExists(privateKeyName!))
                {
                    SavePrivateKey(privateKeyName!, privateKey);
                    result.PrivateKeyAdded = true;
                }
            }
            else
            {
                // Update content with plain value
                content = UpdateEnvContent(content, _key, _value);
            }
            
            // Write updated content
            File.WriteAllText(fullPath, content);
            result.Changed = true;
            result.EnvSrc = content;
        }
        catch (Exception ex)
        {
            result.Error = ex;
        }
        
        return result;
    }
    
    private string UpdateEnvContent(string content, string key, string value)
    {
        var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        var keyPattern = new Regex($@"^\s*(?:export\s+)?{Regex.Escape(key)}\s*=", RegexOptions.Multiline);
        var updated = false;
        
        for (int i = 0; i < lines.Count; i++)
        {
            if (keyPattern.IsMatch(lines[i]))
            {
                // Update existing key
                lines[i] = $"{key}={EscapeValue(value)}";
                updated = true;
                break;
            }
        }
        
        if (!updated)
        {
            // Add new key
            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
            {
                lines.Add(""); // Add empty line before new entry
            }
            lines.Add($"{key}={EscapeValue(value)}");
        }
        
        return string.Join(Environment.NewLine, lines);
    }
    
    private string EscapeValue(string value)
    {
        // If value contains special characters, quote it
        if (value.Contains(' ') || value.Contains('\n') || value.Contains('"') || value.Contains('\''))
        {
            // Use double quotes and escape internal quotes
            var escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return $"\"{escaped}\"";
        }
        
        return value;
    }
    
    private (string publicKey, string privateKey, string privateKeyName) GetOrGenerateKeys(string envFile)
    {
        // Determine key names
        var envName = Path.GetFileNameWithoutExtension(envFile).Replace(".env", "").TrimStart('.');
        var privateKeyName = string.IsNullOrEmpty(envName) ? "DOTENV_PRIVATE_KEY" : $"DOTENV_PRIVATE_KEY_{envName.ToUpper()}";
        var publicKeyName = string.IsNullOrEmpty(envName) ? "DOTENV_PUBLIC_KEY" : $"DOTENV_PUBLIC_KEY_{envName.ToUpper()}";
        
        // Check for existing keys
        var keysFile = _options.EnvKeysFile ?? ".env.keys";
        string? existingPrivateKey = null;
        string? existingPublicKey = null;
        
        if (File.Exists(keysFile))
        {
            var keysContent = File.ReadAllText(keysFile);
            var keys = DotEnvParser.Parse(keysContent, new DotEnvParseOptions());
            keys.TryGetValue(privateKeyName, out existingPrivateKey);
            keys.TryGetValue(publicKeyName, out existingPublicKey);
        }
        
        if (!string.IsNullOrEmpty(existingPrivateKey) && !string.IsNullOrEmpty(existingPublicKey))
        {
            return (existingPublicKey, existingPrivateKey, privateKeyName);
        }
        
        // Generate new keypair
        var keyPair = DotEnvEncryption.GenerateKeyPair();
        
        // Save public key in the env file (as comment)
        var envContent = File.Exists(envFile) ? File.ReadAllText(envFile) : "";
        if (!envContent.Contains($"#{publicKeyName}"))
        {
            envContent = $"#{publicKeyName}=\"{keyPair.PublicKey}\"\n" + envContent;
            File.WriteAllText(envFile, envContent);
        }
        
        return (keyPair.PublicKey, keyPair.PrivateKey, privateKeyName);
    }
    
    private bool KeyExists(string keyName)
    {
        var keysFile = _options.EnvKeysFile ?? ".env.keys";
        if (!File.Exists(keysFile))
            return false;
            
        var keysContent = File.ReadAllText(keysFile);
        var keys = DotEnvParser.Parse(keysContent, new DotEnvParseOptions());
        return keys.ContainsKey(keyName);
    }
    
    private void SavePrivateKey(string keyName, string privateKey)
    {
        var keysFile = _options.EnvKeysFile ?? ".env.keys";
        var content = File.Exists(keysFile) ? File.ReadAllText(keysFile) : "";
        
        // Add to .gitignore if not already there
        AddToGitignore(".env.keys");
        
        // Update or add the key
        content = UpdateEnvContent(content, keyName, privateKey);
        File.WriteAllText(keysFile, content);
    }
    
    private void AddToGitignore(string pattern)
    {
        const string gitignorePath = ".gitignore";
        
        if (!File.Exists(gitignorePath))
        {
            File.WriteAllText(gitignorePath, pattern + Environment.NewLine);
            return;
        }
        
        var content = File.ReadAllText(gitignorePath);
        if (!content.Contains(pattern))
        {
            File.AppendAllText(gitignorePath, Environment.NewLine + pattern + Environment.NewLine);
        }
    }
}