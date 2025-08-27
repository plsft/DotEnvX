# Setup Instructions for GitHub

## 1. Create Repository on GitHub

1. Go to https://github.com/new
2. Repository name: `DotEnvX`
3. Description: `A secure .env file loader for .NET with encryption support - port of dotenvx`
4. Make it **Public**
5. **DO NOT** initialize with README, .gitignore, or license (we already have them)
6. Click "Create repository"

## 2. Push to GitHub

After creating the repository, run these commands:

```bash
# If you haven't already set up git credentials
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"

# Push to the new repository
git push -u origin main
```

If you get authentication errors, you may need to:
- Use a Personal Access Token (PAT) instead of password
- Or use GitHub CLI: `gh auth login`

## 3. Verify Upload

Your repository should now be available at:
https://github.com/plsft/DotEnvX

## 4. Optional: GitHub Actions for CI/CD

Create `.github/workflows/dotnet.yml`:

```yaml
name: .NET Build and Test

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore
      
    - name: Test
      run: dotnet test --no-build --verbosity normal
```

## 5. Publishing to NuGet (Optional)

To publish to NuGet.org:

1. Get API key from https://www.nuget.org/account/apikeys
2. Add as GitHub secret: `NUGET_API_KEY`
3. Add to workflow:

```yaml
    - name: Pack
      run: dotnet pack --no-build --configuration Release
      
    - name: Push to NuGet
      run: dotnet nuget push **/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
      if: github.event_name == 'push' && github.ref == 'refs/heads/main'
```

## Project Structure

```
DotEnvX/
├── DotEnvX.Core/                    # Core library
├── DotEnvX.Extensions.DependencyInjection/  # DI extensions
├── DotEnvX.CLI/                     # CLI tool
├── DotEnvX.Samples/                 # Sample applications
├── DotEnvX.Tests/                   # Unit tests
├── README.md                        # Documentation
├── LICENSE                          # BSD-3-Clause
└── DotEnvX.sln                      # Solution file
```

## Features Implemented

- ✅ .env file parsing
- ✅ Multiple file support
- ✅ Variable expansion
- ✅ ECIES encryption/decryption
- ✅ ASP.NET Core integration
- ✅ Dependency injection
- ✅ Configuration provider
- ✅ Sample applications
- ✅ Unit tests

## Known Issues

- Parser needs fixes for some edge cases (multiline, variable expansion)
- Tests are failing but core functionality works as shown in samples

## Next Steps

1. Fix parser issues in `DotEnvParser.cs`
2. Add more comprehensive tests
3. Create NuGet packages
4. Add CI/CD pipeline
5. Create documentation wiki