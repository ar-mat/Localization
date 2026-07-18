# Armat Localization Library

[![Version](https://img.shields.io/badge/version-3.0.0-blue.svg)](https://github.com/ar-mat/Localization)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)

`Armat.Localization` is a comprehensive and lightweight .NET library that enables robust localization for .NET applications. It provides a complete solution for internationalizing your applications with support for multiple languages and cultures across plain .NET, WPF, and MAUI projects.

## 🌟 Features

- **Multi-format localization support**
  - Simple text localization for any .NET application
  - WPF Resource Dictionary localization
  - MAUI Resource Dictionary localization (Android / iOS / Mac Catalyst / Windows)
  - Extensible architecture for additional formats
- **Runtime language switching** — change languages dynamically without restarting
- **Comprehensive language management** — list supported languages and manage locale information
- **Visual translation tools** — desktop application for easy translation management, including MAUI files
- **Cross-platform support** — works with .NET 10 and later
- **Lightweight and fast** — minimal dependencies and optimized performance

## 📦 Components

### Core Libraries

| Component | Description | NuGet Package |
|-----------|-------------|---------------|
| **[Armat.Localization.Core](Projects/Localization.Core)** | Core localization functionality for all .NET applications | `armat.localization.core` |
| **[Armat.Localization.Wpf](Projects/Localization.Wpf)** | WPF-specific localization for Resource Dictionaries | `armat.localization.wpf` |
| **[Armat.Localization.Maui](Projects/Localization.Maui)** | MAUI-specific localization for Resource Dictionaries | `armat.localization.maui` |

### Tools & Applications

| Component | Description | Download |
|-----------|-------------|----------|
| **[Localization.Designer](Projects/Localization.Designer)** | WPF application for managing translations (string dictionary, WPF, and MAUI formats) | [Releases](https://github.com/ar-mat/Localization/releases) |

### Demo Applications

| Component | Description |
|-----------|-------------|
| **[Demo.ClassLibrary](Projects/Demo/ClassLibrary)** | Example library showing core localization usage |
| **[Demo.WpfApp](Projects/Demo/WpfApp)** | Complete WPF application demonstrating all features |
| **[Demo.MauiApp](Projects/Demo/MauiApp)** | Complete MAUI application demonstrating cross-platform localization |

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

For MAUI applications, also install:

```bash
dotnet add package armat.localization.maui
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

// 2. Create a localizable string dictionary
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

- **[Core Library Documentation](Projects/Localization.Core/Readme.md)** — detailed API reference and usage patterns
- **[WPF Library Documentation](Projects/Localization.Wpf/Readme.md)** — WPF-specific implementation guide
- **[MAUI Library Documentation](Projects/Localization.Maui/Readme.md)** — MAUI-specific implementation guide
- **[Localization Designer](Projects/Localization.Designer/README.md)** — translation management tool
- **[Demo Applications](Projects/Demo)** — complete working examples

## 🏗️ Solution Structure

```
Armat.Localization/
├── Projects/
│   ├── Localization.Core/          # Core library (.NET 10)
│   ├── Localization.Wpf/           # WPF extensions (.NET 10-windows)
│   ├── Localization.Maui/          # MAUI extensions (.NET 10 multi-target)
│   ├── Localization.Designer/      # Translation management tool
│   └── Demo/                       # Example applications
│       ├── ClassLibrary/           # Core usage example
│       ├── WpfApp/                 # WPF usage example
│       └── MauiApp/                # MAUI usage example
├── BuildScripts/                   # Build and packaging scripts
├── Solution/                       # Visual Studio solution files
└── bin/                            # Build output directory
```

The solution file is at `Solution/Armat.Localization/Armat.Localization.sln` — there is no `.sln` at the repository root.

## 🔧 Build & Development

### Prerequisites

- **.NET 10 SDK** or later
- **Visual Studio 2022 17.10+** (for WPF projects) or VS Code
- **MAUI workload** installed: `dotnet workload install maui` (for the MAUI library and demo)
- **PowerShell** (for build scripts)

### Building the Solution

```bash
# Build all projects
dotnet build Solution/Armat.Localization/Armat.Localization.sln

# Build a specific configuration
dotnet build Solution/Armat.Localization/Armat.Localization.sln -c Release

# Build a single project
dotnet build Projects/Localization.Core/Localization.Core.csproj
```

There are no test projects in the solution; `dotnet test` is a no-op.

### Packaging

Use the provided PowerShell scripts (run from inside `BuildScripts/`):

```powershell
cd BuildScripts

# Pack NuGet packages (Core + Wpf + Maui in Release by default)
.\Pack.ps1

# Publish applications (Core + Wpf + Maui + Designer + zips)
.\Publish.ps1
```

## 🤝 Contributing

We welcome contributions. Please see [Contributing Guidelines](CONTRIBUTING.md) for:

- Code style and standards (naming, formatting, comments)
- Pull request process
- Issue reporting
- Development setup

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE.txt) file for details.

## 🙏 Acknowledgments

- Built by [Ara Petrosyan](https://github.com/ar-mat)
- Inspired by the need for simple yet powerful localization tools
- Thanks to all contributors and users of the library

## 📞 Support

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/ar-mat/Localization/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/ar-mat/Localization/discussions)
- 🌐 **Project Website**: [armat.am/products/localization](http://armat.am/products/localization)
