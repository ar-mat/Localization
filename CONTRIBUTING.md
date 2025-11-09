# Contributing to Armat Localization

Thank you for contributing! This document covers the essential guidelines for contributing to the Armat Localization library.

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- Git
- Visual Studio 2022 or VS Code

### Setup
```bash
git clone https://github.com/yourusername/Localization.git
cd Localization
dotnet restore Solution/Armat.Localization/Armat.Localization.sln
dotnet build Solution/Armat.Localization/Armat.Localization.sln
```

## 📋 Code Style

### Naming Conventions
- **Classes/Methods/Properties**: `PascalCase`
- **Local variables/parameters**: `camelCase` 
- **Private fields**: `_camelCase`

### Formatting
- **Braces**: Allman style (new line)
- **Indentation**: Tabs
- **Types**: Use system types (`String`, `Int32`, `Boolean`)

### Example
```csharp
public class LocalizationManager
{
	private String _currentLanguage;
	
	public String GetTranslation(String key)
	{
		if (String.IsNullOrEmpty(key))
		{
			return String.Empty;
		}
		return _translations[key];
	}
}
```

## 🔄 Workflow

**Important**: All contributions must be submitted via pull requests. Direct commits to the main branch are not allowed.

1. **Fork** the repository to your GitHub account
4. **Make changes** following the code style guidelines
5. **Test** your changes thoroughly
6. **Commit** with clear, descriptive messages: `feat(core): add new feature`
7. **Push** your branch to your fork
8. **Submit a pull request** to the main repository's `main` branch
9. **Wait for review** and address any feedback from maintainers

### Pull Request Guidelines
- Provide a clear title and description
- Reference any related issues using `#issue-number`
- Ensure all tests pass
- Keep pull requests focused on a single feature or fix
- Update documentation if necessary

## 🐛 Issues & Features

### Bug Reports
Include:
- Steps to reproduce
- Expected vs actual behavior
- Environment details (.NET version, OS)

### Feature Requests
Include:
- Problem description
- Proposed solution
- Use cases

## 📝 Documentation

- Document public APIs with XML comments
- Update README files if needed
- Include code examples

---

Questions? Open an issue or start a discussion.