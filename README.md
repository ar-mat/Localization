# Armat Localization Library

[![Version](https://img.shields.io/badge/version-2.0.1-blue.svg)](https://github.com/ar-mat/Localization)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)

`Armat.Localization` is a comprehensive and lightweight .NET library that enables robust localization for .NET applications. It provides a complete solution for internationalizing your applications with support for multiple languages and cultures.

## 🌟 Features

- **Multi-format localization support**
  - Simple text localization for any .NET application
  - WPF Resource Dictionary localization
  - Extensible architecture for additional formats
- **Runtime language switching** - Change languages dynamically without restarting
- **Comprehensive language management** - List supported languages and manage locale information
- **Visual translation tools** - Desktop application for easy translation management
- **Cross-platform support** - Works with .NET 8.0 and later
- **Lightweight and fast** - Minimal dependencies and optimized performance

## 📦 Components

### Core Libraries

| Component | Description | NuGet Package |
|-----------|-------------|---------------|
| **[Armat.Localization.Core](Projects/Localization.Core)** | Core localization functionality for all .NET applications | `armat.localization.core` |
| **[Armat.Localization.Wpf](Projects/Localization.Wpf)** | WPF-specific localization for Resource Dictionaries | `armat.localization.wpf` |

### Tools & Applications

| Component | Description | Download |
|-----------|-------------|----------|
| **[Localization.Designer](Projects/Localization.Designer)** | WPF application for managing translations | [Releases](https://github.com/ar-mat/Localization/releases) |

### Demo Applications

| Component | Description |
|-----------|-------------|
| **[Demo.ClassLibrary](Projects/Demo/ClassLibrary)** | Example library showing core localization usage |
| **[Demo.WpfApp](Projects/Demo/WpfApp)** | Complete WPF application demonstrating all features |

## 🚀 Quick Start

### Installation

Install the core package via NuGet Package Manager:

```bash
dotnet add package armat.localization.core
```

For WPF applications, also install:

```bash
dotnet add package armat.localization.wpf
```

### Basic Usage

```csharp
// 1. Configure and create LocalizationManager
var config = new Configuration
{
    DefaultLocale = new LocaleInfo("en"),
    TranslationsDirectoryPath = "Localization"
};
var localizationManager = LocalizationManager.CreateDefaultInstance(config);

// 2. Create localizable string dictionary
var stringDict = new LocalizableStringDictionary
{
    LocalizationManager = LocalizationManager.Default,
    Source = new Uri("YourAssembly;component/Localization/Strings.xaml", UriKind.Relative)
};

// 3. Switch languages at runtime
LocalizationManager.Default.ChangeLocale("fr");

// 4. Get localized strings
string localizedText = stringDict.GetValueOrDefault("WelcomeMessage", "Welcome!");
```

## 📚 Documentation

- **[Core Library Documentation](Projects/Localization.Core/Readme.md)** - Detailed API reference and usage patterns
- **[WPF Library Documentation](Projects/Localization.Wpf/Readme.md)** - WPF-specific implementation guide
- **[Demo Applications](Projects/Demo)** - Complete working examples

## 🏗️ Solution Structure

```
Armat.Localization/
├── Projects/
│   ├── Localization.Core/          # Core library (.NET 8.0)
│   ├── Localization.Wpf/           # WPF extensions (.NET 8.0-windows)
│   ├── Localization.Designer/      # Translation management tool
│   └── Demo/                       # Example applications
│       ├── ClassLibrary/           # Core usage example
│       └── WpfApp/                 # WPF usage example
├── BuildScripts/                   # Build and packaging scripts
├── Solution/                       # Visual Studio solution files
└── bin/                            # Build output directory
```

## 🔧 Build & Development

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 (for WPF projects) or VS Code
- PowerShell (for build scripts)

### Building the Solution

```bash
# Build all projects
dotnet build Solution/Armat.Localization/Armat.Localization.sln

# Build specific configuration
dotnet build -c Release

# Run tests (if available)
dotnet test
```

### Packaging

Use the provided PowerShell scripts:

```powershell
# Package NuGet packages
.\BuildScripts\Pack.ps1

# Publish applications
.\BuildScripts\Publish.ps1
```

## 🤝 Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on:

- Code style and standards
- Pull request process
- Issue reporting
- Development setup

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ by [Ara Petrosyan](https://github.com/ar-mat)
- Inspired by the need for simple yet powerful localization tools
- Thanks to all contributors and users of the library

## 📞 Support

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/ar-mat/Localization/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/ar-mat/Localization/discussions)
- 🌐 **Project Website**: [armat.am/products/localization](http://armat.am/products/localization)