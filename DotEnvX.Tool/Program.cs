using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using DotEnvX.Core;
using DotEnvX.Core.Models;
using Spectre.Console;

namespace DotEnvX.Tool;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("DotEnvX CLI - Manage .env files with encryption support")
        {
            TreatUnmatchedTokensAsErrors = false
        };

        // Add commands
        rootCommand.AddCommand(CreateSetCommand());
        rootCommand.AddCommand(CreateGetCommand());
        rootCommand.AddCommand(CreateListCommand());
        rootCommand.AddCommand(CreateEncryptCommand());
        rootCommand.AddCommand(CreateDecryptCommand());
        rootCommand.AddCommand(CreateKeypairCommand());
        rootCommand.AddCommand(CreateExampleCommand());
        rootCommand.AddCommand(CreateRunCommand());
        rootCommand.AddCommand(CreateValidateCommand());

        // Global options
        var fileOption = new Option<string>(
            new[] { "--file", "-f" },
            getDefaultValue: () => ".env",
            description: "Path to .env file");
        rootCommand.AddGlobalOption(fileOption);

        return await rootCommand.InvokeAsync(args);
    }

    static Command CreateSetCommand()
    {
        var cmd = new Command("set", "Set one or more environment variables")
        {
            TreatUnmatchedTokensAsErrors = false
        };

        var encryptOption = new Option<bool>(
            new[] { "--encrypt", "-e" },
            getDefaultValue: () => false,
            description: "Encrypt the value before saving");

        var forceOption = new Option<bool>(
            new[] { "--force" },
            getDefaultValue: () => false,
            description: "Overwrite existing values");

        cmd.AddOption(encryptOption);
        cmd.AddOption(forceOption);

        // Allow multiple key=value arguments
        var keyValueArgs = new Argument<string[]>(
            "key=value",
            "One or more key=value pairs to set")
        {
            Arity = ArgumentArity.OneOrMore
        };
        cmd.AddArgument(keyValueArgs);

        cmd.Handler = CommandHandler.Create<string, bool, bool, string[]>((file, encrypt, force, keyValueArgs) =>
        {
            return HandleSet(file, encrypt, force, keyValueArgs);
        });

        return cmd;
    }

    static Command CreateGetCommand()
    {
        var cmd = new Command("get", "Get the value of an environment variable");
        
        var keyArg = new Argument<string>("key", "The environment variable key to retrieve");
        cmd.AddArgument(keyArg);

        cmd.Handler = CommandHandler.Create<string, string>((file, key) =>
        {
            return HandleGet(file, key);
        });

        return cmd;
    }

    static Command CreateListCommand()
    {
        var cmd = new Command("list", "List all environment variables");
        cmd.AddAlias("ls");

        var showValuesOption = new Option<bool>(
            new[] { "--values", "-v" },
            getDefaultValue: () => false,
            description: "Show values (default: hide sensitive values)");

        var jsonOption = new Option<bool>(
            new[] { "--json" },
            getDefaultValue: () => false,
            description: "Output as JSON");

        cmd.AddOption(showValuesOption);
        cmd.AddOption(jsonOption);

        cmd.Handler = CommandHandler.Create<string, bool, bool>((file, values, json) =>
        {
            return HandleList(file, values, json);
        });

        return cmd;
    }

    static Command CreateEncryptCommand()
    {
        var cmd = new Command("encrypt", "Encrypt all values in the .env file");

        var keyOption = new Option<string[]>(
            new[] { "--keys", "-k" },
            description: "Specific keys to encrypt (default: all)")
        {
            AllowMultipleArgumentsPerToken = true
        };

        cmd.AddOption(keyOption);

        cmd.Handler = CommandHandler.Create<string, string[]?>((file, keys) =>
        {
            return HandleEncrypt(file, keys);
        });

        return cmd;
    }

    static Command CreateDecryptCommand()
    {
        var cmd = new Command("decrypt", "Decrypt and display values");

        var outputOption = new Option<string>(
            new[] { "--output", "-o" },
            description: "Output to file instead of console");

        cmd.AddOption(outputOption);

        cmd.Handler = CommandHandler.Create<string, string?>((file, output) =>
        {
            return HandleDecrypt(file, output);
        });

        return cmd;
    }

    static Command CreateKeypairCommand()
    {
        var cmd = new Command("keypair", "Generate a new public/private keypair");

        var saveOption = new Option<bool>(
            new[] { "--save", "-s" },
            getDefaultValue: () => false,
            description: "Save keys to .env and .env.keys files");

        cmd.AddOption(saveOption);

        cmd.Handler = CommandHandler.Create<string, bool>((file, save) =>
        {
            return HandleKeypair(file, save);
        });

        return cmd;
    }

    static Command CreateExampleCommand()
    {
        var cmd = new Command("example", "Generate .env.example from .env file");

        cmd.Handler = CommandHandler.Create<string>((file) =>
        {
            return HandleExample(file);
        });

        return cmd;
    }

    static Command CreateRunCommand()
    {
        var cmd = new Command("run", "Run a command with .env loaded")
        {
            TreatUnmatchedTokensAsErrors = false
        };

        var commandArg = new Argument<string[]>("command", "Command to execute")
        {
            Arity = ArgumentArity.OneOrMore
        };
        cmd.AddArgument(commandArg);

        cmd.Handler = CommandHandler.Create<string, string[]>((file, command) =>
        {
            return HandleRun(file, command);
        });

        return cmd;
    }

    static Command CreateValidateCommand()
    {
        var cmd = new Command("validate", "Validate .env file syntax");

        cmd.Handler = CommandHandler.Create<string>((file) =>
        {
            return HandleValidate(file);
        });

        return cmd;
    }

    // Handler implementations
    static int HandleSet(string file, bool encrypt, bool force, string[] keyValuePairs)
    {
        try
        {
            AnsiConsole.MarkupLine($"[cyan]Setting values in {file}[/]");

            var setCount = 0;
            var errors = new List<string>();

            foreach (var kvp in keyValuePairs)
            {
                var parts = kvp.Split('=', 2);
                if (parts.Length != 2)
                {
                    errors.Add($"Invalid format: {kvp} (expected KEY=value)");
                    continue;
                }

                var key = parts[0].Trim();
                var value = parts[1].Trim();

                if (string.IsNullOrWhiteSpace(key))
                {
                    errors.Add($"Empty key in: {kvp}");
                    continue;
                }

                try
                {
                    // Check if key exists
                    if (!force && File.Exists(file))
                    {
                        var existing = DotEnv.Parse(File.ReadAllText(file));
                        if (existing.ContainsKey(key))
                        {
                            AnsiConsole.MarkupLine($"[yellow]⚠ Key '{key}' already exists. Use --force to overwrite[/]");
                            continue;
                        }
                    }

                    // Set the value
                    var result = DotEnv.Set(key, value, new SetOptions
                    {
                        Path = new[] { file },
                        Encrypt = encrypt
                    });

                    if (encrypt)
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] {key} = [dim](encrypted)[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[green]✓[/] {key} = {value}");
                    }
                    setCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Failed to set {key}: {ex.Message}");
                }
            }

            if (errors.Any())
            {
                AnsiConsole.MarkupLine("[red]Errors occurred:[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
                }
            }

            AnsiConsole.MarkupLine($"\n[green]Successfully set {setCount} value(s)[/]");
            return errors.Any() ? 1 : 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleGet(string file, string key)
    {
        try
        {
            // Load the file
            DotEnv.Config(new DotEnvOptions
            {
                Path = new[] { file },
                Strict = false
            });

            var value = Environment.GetEnvironmentVariable(key);
            
            if (value != null)
            {
                Console.WriteLine(value);
                return 0;
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]Key '{key}' not found in {file}[/]");
                return 1;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleList(string file, bool showValues, bool json)
    {
        try
        {
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {file}[/]");
                return 1;
            }

            var content = File.ReadAllText(file);
            var parsed = DotEnv.Parse(content);

            if (json)
            {
                if (showValues)
                {
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(parsed, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }
                else
                {
                    var hidden = parsed.ToDictionary(k => k.Key, v => "***");
                    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(hidden, new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true
                    }));
                }
            }
            else
            {
                var table = new Table();
                table.AddColumn("Key");
                table.AddColumn("Value");
                table.AddColumn("Type");

                foreach (var kvp in parsed.OrderBy(k => k.Key))
                {
                    var value = showValues ? kvp.Value : MaskValue(kvp.Key, kvp.Value);
                    var type = DetectType(kvp.Key, kvp.Value);
                    
                    table.AddRow(
                        $"[cyan]{kvp.Key}[/]",
                        value,
                        $"[dim]{type}[/]"
                    );
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[dim]Total: {parsed.Count} variable(s)[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleEncrypt(string file, string[]? keys)
    {
        try
        {
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {file}[/]");
                return 1;
            }

            // Check for or generate keypair
            var keypair = EnsureKeypair(file);
            
            var content = File.ReadAllText(file);
            var parsed = DotEnv.Parse(content);
            
            var toEncrypt = keys?.Any() == true 
                ? parsed.Where(kvp => keys.Contains(kvp.Key))
                : parsed;

            var encrypted = 0;
            foreach (var kvp in toEncrypt)
            {
                if (!kvp.Value.StartsWith("encrypted:"))
                {
                    DotEnv.Set(kvp.Key, kvp.Value, new SetOptions
                    {
                        Path = new[] { file },
                        Encrypt = true
                    });
                    AnsiConsole.MarkupLine($"[green]✓[/] Encrypted {kvp.Key}");
                    encrypted++;
                }
            }

            AnsiConsole.MarkupLine($"\n[green]Encrypted {encrypted} value(s)[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleDecrypt(string file, string? output)
    {
        try
        {
            // Load with decryption
            var result = DotEnv.Config(new DotEnvOptions
            {
                Path = new[] { file },
                Strict = false
            });

            if (result.Parsed == null)
            {
                AnsiConsole.MarkupLine($"[red]Failed to parse {file}[/]");
                return 1;
            }

            if (output != null)
            {
                // Write decrypted to file
                var lines = result.Parsed.Select(kvp => $"{kvp.Key}={kvp.Value}");
                File.WriteAllLines(output, lines);
                AnsiConsole.MarkupLine($"[green]✓[/] Decrypted values written to {output}");
            }
            else
            {
                // Display decrypted values
                var table = new Table();
                table.AddColumn("Key");
                table.AddColumn("Decrypted Value");

                foreach (var kvp in result.Parsed.OrderBy(k => k.Key))
                {
                    table.AddRow(kvp.Key, kvp.Value);
                }

                AnsiConsole.Write(table);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleKeypair(string file, bool save)
    {
        try
        {
            var keypair = DotEnv.GenerateKeypair();

            AnsiConsole.MarkupLine("[cyan]Generated new keypair:[/]");
            AnsiConsole.MarkupLine($"[green]Public Key:[/]  {keypair.PublicKey}");
            AnsiConsole.MarkupLine($"[yellow]Private Key:[/] {keypair.PrivateKey}");

            if (save)
            {
                // Save public key as comment in .env
                var envContent = File.Exists(file) ? File.ReadAllText(file) : "";
                if (!envContent.Contains("#DOTENV_PUBLIC_KEY"))
                {
                    envContent = $"#DOTENV_PUBLIC_KEY=\"{keypair.PublicKey}\"\n" + envContent;
                    File.WriteAllText(file, envContent);
                    AnsiConsole.MarkupLine($"[green]✓[/] Public key saved to {file}");
                }

                // Save private key to .env.keys
                var keysFile = ".env.keys";
                var keysContent = File.Exists(keysFile) ? File.ReadAllText(keysFile) : "";
                if (!keysContent.Contains("DOTENV_PRIVATE_KEY"))
                {
                    keysContent += $"\nDOTENV_PRIVATE_KEY=\"{keypair.PrivateKey}\"\n";
                    File.WriteAllText(keysFile, keysContent.Trim() + "\n");
                    AnsiConsole.MarkupLine($"[green]✓[/] Private key saved to {keysFile}");

                    // Add to .gitignore
                    AddToGitignore(".env.keys");
                }
            }
            else
            {
                AnsiConsole.MarkupLine("\n[dim]Use --save to save these keys to files[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleExample(string file)
    {
        try
        {
            var result = DotEnv.GenExample(Path.GetDirectoryName(file) ?? ".", Path.GetFileName(file));
            
            AnsiConsole.MarkupLine($"[green]✓[/] Generated {result.ExampleFilepath}");
            AnsiConsole.MarkupLine($"[dim]Added {result.AddedKeys.Count} keys[/]");
            
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleRun(string file, string[] command)
    {
        try
        {
            // Load environment
            DotEnv.Config(new DotEnvOptions
            {
                Path = new[] { file },
                Overload = true
            });

            // Execute command
            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command[0],
                Arguments = string.Join(" ", command.Skip(1)),
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            var process = System.Diagnostics.Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                return process.ExitCode;
            }

            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    static int HandleValidate(string file)
    {
        try
        {
            if (!File.Exists(file))
            {
                AnsiConsole.MarkupLine($"[red]File not found: {file}[/]");
                return 1;
            }

            var content = File.ReadAllText(file);
            var lines = content.Split('\n');
            var errors = new List<string>();
            var warnings = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                
                // Skip empty lines and comments
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                // Check for valid key=value format
                if (!line.Contains('='))
                {
                    errors.Add($"Line {i + 1}: Missing '=' separator");
                    continue;
                }

                var parts = line.Split('=', 2);
                var key = parts[0].Trim();
                
                // Validate key format
                if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    errors.Add($"Line {i + 1}: Invalid key format '{key}'");
                }

                // Check for common issues
                if (key.Contains(' '))
                {
                    errors.Add($"Line {i + 1}: Key contains spaces");
                }
            }

            // Try to parse
            try
            {
                var parsed = DotEnv.Parse(content);
                AnsiConsole.MarkupLine($"[green]✓[/] Successfully parsed {parsed.Count} variables");
            }
            catch (Exception ex)
            {
                errors.Add($"Parse error: {ex.Message}");
            }

            if (errors.Any())
            {
                AnsiConsole.MarkupLine("[red]Validation errors:[/]");
                foreach (var error in errors)
                {
                    AnsiConsole.MarkupLine($"  [red]✗[/] {error}");
                }
                return 1;
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓ {file} is valid[/]");
                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return 1;
        }
    }

    // Helper methods
    static string MaskValue(string key, string value)
    {
        if (IsSensitive(key))
        {
            return value.Length > 4 
                ? value.Substring(0, 2) + new string('*', Math.Min(value.Length - 4, 10)) + value.Substring(value.Length - 2)
                : new string('*', value.Length);
        }
        return value;
    }

    static bool IsSensitive(string key)
    {
        var sensitivePatterns = new[] { "KEY", "SECRET", "PASSWORD", "TOKEN", "CREDENTIAL", "PRIVATE" };
        return sensitivePatterns.Any(pattern => key.ToUpper().Contains(pattern));
    }

    static string DetectType(string key, string value)
    {
        if (value.StartsWith("encrypted:")) return "encrypted";
        if (IsSensitive(key)) return "sensitive";
        if (bool.TryParse(value, out _)) return "boolean";
        if (int.TryParse(value, out _)) return "number";
        if (Uri.TryCreate(value, UriKind.Absolute, out _)) return "url";
        return "string";
    }

    static DotEnvX.Core.Encryption.DotEnvEncryption.KeyPair EnsureKeypair(string file)
    {
        // Check for existing keys
        if (File.Exists(".env.keys"))
        {
            var keysContent = File.ReadAllText(".env.keys");
            var keys = DotEnv.Parse(keysContent);
            if (keys.TryGetValue("DOTENV_PRIVATE_KEY", out var privateKey) &&
                keys.TryGetValue("DOTENV_PUBLIC_KEY", out var publicKey))
            {
                return new DotEnvX.Core.Encryption.DotEnvEncryption.KeyPair
                {
                    PrivateKey = privateKey,
                    PublicKey = publicKey
                };
            }
        }

        // Generate new keypair
        AnsiConsole.MarkupLine("[yellow]No keypair found. Generating new keypair...[/]");
        var keypair = DotEnv.GenerateKeypair();
        
        // Save keys
        File.WriteAllText(".env.keys", $"DOTENV_PRIVATE_KEY={keypair.PrivateKey}\nDOTENV_PUBLIC_KEY={keypair.PublicKey}\n");
        AddToGitignore(".env.keys");
        
        // Add public key to .env
        var envContent = File.Exists(file) ? File.ReadAllText(file) : "";
        if (!envContent.Contains("#DOTENV_PUBLIC_KEY"))
        {
            envContent = $"#DOTENV_PUBLIC_KEY=\"{keypair.PublicKey}\"\n" + envContent;
            File.WriteAllText(file, envContent);
        }

        return keypair;
    }

    static void AddToGitignore(string pattern)
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
