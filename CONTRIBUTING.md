# Contributing to DotEnvX

Thank you for your interest in contributing to DotEnvX! We welcome contributions from the community and are excited to see what you'll bring to the project.

## Code of Conduct

Please note that this project adheres to a code of conduct. By participating, you are expected to uphold this code:

- Use welcoming and inclusive language
- Be respectful of differing viewpoints and experiences
- Gracefully accept constructive criticism
- Focus on what is best for the community
- Show empathy towards other community members

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed and what behavior you expected**
- **Include screenshots if relevant**
- **Include your environment details** (.NET version, OS, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a detailed description of the suggested enhancement**
- **Provide specific examples to demonstrate the enhancement**
- **Describe the current behavior and explain the expected behavior**
- **Explain why this enhancement would be useful**

### Pull Requests

1. **Fork the repository** and create your branch from `main`
2. **Follow the existing code style** - we use standard C# conventions
3. **Write tests** for any new functionality
4. **Ensure all tests pass** by running `dotnet test`
5. **Update documentation** as needed
6. **Create a Pull Request** with a clear title and description

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or Rider
- Git

### Getting Started

1. Clone your fork:
```bash
git clone https://github.com/yourusername/DotEnvX.git
cd DotEnvX
```

2. Build the solution:
```bash
dotnet build
```

3. Run tests:
```bash
dotnet test
```

4. Install the CLI tool locally:
```bash
cd DotEnvX.Tool
dotnet pack
dotnet tool install --global --add-source ./nupkg DotEnvX.Tool
```

## Project Structure

```
DotEnvX/
├── DotEnvX.Core/                          # Core library
│   ├── Parser/                            # .env file parser
│   ├── Encryption/                        # Encryption implementation
│   ├── Services/                          # Core services
│   └── Models/                            # Data models
├── DotEnvX.Extensions.DependencyInjection/ # ASP.NET Core integration
├── DotEnvX.Tool/                          # CLI tool
├── DotEnvX.Samples/                       # Sample applications
└── DotEnvX.Tests/                         # Unit tests
```

## Coding Guidelines

### C# Style

- Use meaningful variable and method names
- Keep methods small and focused on a single responsibility
- Use async/await for I/O operations
- Handle exceptions appropriately
- Add XML documentation comments for public APIs

### Commit Messages

- Use the present tense ("Add feature" not "Added feature")
- Use the imperative mood ("Move cursor to..." not "Moves cursor to...")
- Limit the first line to 72 characters or less
- Reference issues and pull requests liberally after the first line

Examples:
```
Add encryption support for vault files

- Implement vault file format
- Add VaultEncryption service
- Update CLI commands

Fixes #123
```

### Testing

- Write unit tests for all new functionality
- Aim for high code coverage
- Use descriptive test names that explain what is being tested
- Follow the Arrange-Act-Assert pattern

Example:
```csharp
[Fact]
public void Parse_WithValidEnvContent_ReturnsExpectedDictionary()
{
    // Arrange
    var content = "KEY=value\nANOTHER=test";
    
    // Act
    var result = DotEnvParser.Parse(content);
    
    // Assert
    Assert.Equal("value", result["KEY"]);
    Assert.Equal("test", result["ANOTHER"]);
}
```

## Areas Needing Help

We're particularly interested in contributions in these areas:

- **Performance optimizations** - Improving parse speed and memory usage
- **Additional encryption algorithms** - Supporting more encryption methods
- **Cloud integrations** - Azure Key Vault, AWS Secrets Manager
- **IDE extensions** - VS Code, Visual Studio extensions
- **Documentation** - Tutorials, guides, API documentation
- **Internationalization** - Multi-language support for CLI
- **Bug fixes** - Especially parser edge cases

## Release Process

1. Update version numbers in `.csproj` files
2. Update CHANGELOG.md with release notes
3. Create a release tag: `git tag -a v1.0.0 -m "Release version 1.0.0"`
4. Push to GitHub: `git push origin v1.0.0`
5. GitHub Actions will automatically publish to NuGet

## Questions?

Feel free to:
- Open an issue for questions
- Start a discussion in GitHub Discussions
- Contact the maintainers

## License

By contributing, you agree that your contributions will be licensed under the BSD 3-Clause License.

## Recognition

Contributors will be recognized in:
- The README.md file
- Release notes
- The project's contributors page on GitHub

Thank you for contributing to DotEnvX! 🎉