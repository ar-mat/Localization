# Build Scripts

This directory contains PowerShell scripts for building, packaging, and publishing the Armat Localization library components. These scripts automate the build process and ensure consistent packaging across all projects.

## 📋 Scripts Overview

| Script | Purpose | Target Projects |
|--------|---------|----------------|
| **[Pack.ps1](Pack.ps1)** | Creates NuGet packages for distribution | Core, WPF libraries |
| **[Publish.ps1](Publish.ps1)** | Publishes applications for deployment | Core, WPF, Designer |
| **[GetAssemblyVersion.ps1](GetAssemblyVersion.ps1)** | Retrieves version information from assemblies | All projects |

## 🚀 Usage

### Prerequisites

- **PowerShell 5.1** or later
- **.NET 8.0 SDK** installed
- **NuGet CLI** (for advanced packaging scenarios)
- **Write permissions** to output directories

### Pack.ps1 - NuGet Package Creation

Creates NuGet packages for the core and WPF libraries.

```powershell
# Pack all libraries (default behavior)
.\Pack.ps1

# Pack in Debug configuration
.\Pack.ps1 -Configuration Debug

# Pack specific project only
.\Pack.ps1 -ProjectName "Localization.Core"

# Pack WPF library only  
.\Pack.ps1 -ProjectName "Localization.Wpf"
```

**Parameters:**
- `Configuration` (optional) - Build configuration (`Release` | `Debug`). Default: `Release`
- `ProjectName` (optional) - Specific project to pack. Default: all packable projects

**Output:**
- NuGet packages (`.nupkg`) in `bin/Release/pack/` directory
- Package symbols (`.snupkg`) for debugging support
- Package verification and validation reports

**Generated Packages:**
- `armat.localization.core.{version}.nupkg` - Core library
- `armat.localization.wpf.{version}.nupkg` - WPF extensions

### Publish.ps1 - Application Publishing

Publishes applications with all dependencies for deployment.

```powershell
# Publish all applications (default behavior)
.\Publish.ps1

# Publish in Debug configuration
.\Publish.ps1 -Configuration Debug

# Publish Designer application only
.\Publish.ps1 -ProjectName "Localization.Designer"

# Publish Core library only
.\Publish.ps1 -ProjectName "Localization.Core"
```

**Parameters:**
- `Configuration` (optional) - Build configuration (`Release` | `Debug`). Default: `Release`
- `ProjectName` (optional) - Specific project to publish. Default: all projects

**Output:**
- Self-contained deployments in `bin/Release/publish/{ProjectName}/` directories
- All dependencies included (framework-dependent or self-contained)
- Ready-to-distribute application bundles

**Published Applications:**
- `armat.localization.core` - Core library assemblies
- `armat.localization.wpf` - WPF library assemblies  
- `armat.localization.designer` - Complete Designer application
