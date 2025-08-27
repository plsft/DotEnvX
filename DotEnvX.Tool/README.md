# DotEnvX CLI Tool

A powerful command-line tool for managing `.env` files with encryption support, built on top of the DotEnvX library.

## Installation

### As a .NET Tool (Global)
```bash
dotnet tool install --global DotEnvX.Tool
```

### As a .NET Tool (Local)
```bash
dotnet new tool-manifest # if not already present
dotnet tool install DotEnvX.Tool
```

## Usage

```bash
dotenvx [command] [options]
```

## Commands

### Set Values
Set one or more environment variables:
```bash
# Set a single value
dotenvx set DATABASE_URL=postgresql://localhost/mydb

# Set multiple values
dotenvx set API_KEY=secret123 DEBUG=true PORT=3000

# Set with encryption
dotenvx set API_SECRET=supersecret --encrypt

# Force overwrite existing values
dotenvx set DATABASE_URL=newurl --force

# Use a different file
dotenvx --file .env.production set API_URL=https://api.example.com
```

### Get Values
Retrieve an environment variable:
```bash
# Get a value
dotenvx get DATABASE_URL

# From a specific file
dotenvx --file .env.production get API_URL
```

### List Variables
List all environment variables:
```bash
# List variables (hides sensitive values)
dotenvx list
dotenvx ls  # alias

# Show all values
dotenvx list --values

# Output as JSON
dotenvx list --json

# Show values from specific file
dotenvx --file .env.production list --values
```

### Encryption

#### Generate Keypair
```bash
# Generate and display keypair
dotenvx keypair

# Generate and save to files
dotenvx keypair --save
```

#### Encrypt Values
```bash
# Encrypt all values in .env
dotenvx encrypt

# Encrypt specific keys
dotenvx encrypt --keys API_KEY DATABASE_URL

# Encrypt in a specific file
dotenvx --file .env.production encrypt
```

#### Decrypt Values
```bash
# Display decrypted values
dotenvx decrypt

# Save decrypted values to file
dotenvx decrypt --output .env.decrypted

# Decrypt from specific file
dotenvx --file .env.encrypted decrypt
```

### Run Commands
Run commands with environment loaded:
```bash
# Run a command with .env loaded
dotenvx run -- node app.js

# Run with specific env file
dotenvx --file .env.production run -- npm start

# Run dotnet application
dotenvx run -- dotnet run
```

### Validate
Check .env file syntax:
```bash
# Validate .env file
dotenvx validate

# Validate specific file
dotenvx --file .env.production validate
```

### Generate Example
Create .env.example from .env:
```bash
# Generate example file
dotenvx example

# From specific file
dotenvx --file .env.production example
```

## Examples

### Basic Workflow
```bash
# 1. Create new .env file
dotenvx set DATABASE_URL=postgresql://localhost/mydb
dotenvx set API_KEY=development_key
dotenvx set DEBUG=true

# 2. List variables
dotenvx list

# 3. Generate example for team
dotenvx example
```

### Encrypted Workflow
```bash
# 1. Generate keypair
dotenvx keypair --save

# 2. Set encrypted values
dotenvx set API_SECRET=supersecret --encrypt
dotenvx set DATABASE_PASSWORD=dbpass123 --encrypt

# 3. View encrypted file
dotenvx list

# 4. Decrypt when needed
dotenvx decrypt
```

### Multiple Environments
```bash
# Development
dotenvx --file .env.development set DATABASE_URL=postgresql://localhost/devdb

# Staging
dotenvx --file .env.staging set DATABASE_URL=postgresql://staging/db

# Production (encrypted)
dotenvx --file .env.production keypair --save
dotenvx --file .env.production set DATABASE_URL=postgresql://prod/db --encrypt
```

## Security Notes

- **Never commit `.env.keys` files** - Contains private keys
- **Public keys can be committed** - Safe to share
- **Use encryption for sensitive values** - API keys, passwords, etc.
- **Add to .gitignore**: `.env`, `.env.local`, `.env.keys`

## File Structure

```
project/
├── .env                 # Main environment file
├── .env.example         # Example file (safe to commit)
├── .env.keys           # Private keys (NEVER commit)
├── .env.production     # Production environment
└── .gitignore          # Include .env* patterns
```

## Global Options

- `-f, --file <path>` - Specify .env file (default: `.env`)
- `-h, --help` - Show help
- `--version` - Show version

## Exit Codes

- `0` - Success
- `1` - Error occurred

## Integration with CI/CD

### GitHub Actions
```yaml
- name: Setup environment
  run: |
    dotnet tool install --global DotEnvX.Tool
    echo "${{ secrets.DOTENV_PRIVATE_KEY }}" > .env.keys
    dotenvx decrypt --output .env
```

### Azure DevOps
```yaml
- script: |
    dotnet tool install --global DotEnvX.Tool
    echo $(DOTENV_PRIVATE_KEY) > .env.keys
    dotenvx decrypt --output .env
  displayName: 'Setup environment'
```

## Troubleshooting

### Command not found
```bash
# Ensure tool is installed
dotnet tool list --global

# Reinstall if needed
dotnet tool uninstall --global DotEnvX.Tool
dotnet tool install --global DotEnvX.Tool
```

### Parse errors
```bash
# Validate syntax
dotenvx validate

# Check for special characters
dotenvx list --json
```

### Encryption issues
```bash
# Ensure keypair exists
dotenvx keypair --save

# Check .env.keys file
cat .env.keys
```

## License

BSD-3-Clause