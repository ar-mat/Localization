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

1. **Fork** the repository
2. **Create branch**: `feature/your-feature` or `bugfix/issue-description`
3. **Make changes** following code style
4. **Test** your changes
5. **Commit** with clear messages: `feat(core): add new feature`
6. **Submit** pull request

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