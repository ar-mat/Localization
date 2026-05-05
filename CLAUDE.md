# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution layout

The solution is **`Solution/Armat.Localization/Armat.Localization.sln`** — there is no .sln at the repo root. All `dotnet` commands targeting the whole solution must reference that path.

Top-level shape:

- `Projects/Localization.Core/` — runtime-agnostic core library (`armat.localization.core`).
- `Projects/Localization.Wpf/` — WPF `LocalizableResourceDictionary` (`armat.localization.wpf`, `net*-windows`, `UseWPF`).
- `Projects/Localization.Maui/` — MAUI `LocalizableResourceDictionary` (`armat.localization.maui`, multi-targets `-android`, `-ios`, `-maccatalyst`, `-windows10.0.19041.0`).
- `Projects/Localization.Designer/` — WPF + WinForms desktop translator GUI (`armat.localization.designer`).
- `Projects/Demo/{ClassLibrary,WpfApp,MauiApp}/` — usage examples wired into the .sln.
- `Projects/Localization.Import.csproj` — **shared MSBuild props imported by every project**. Not a buildable project. Edit this to bump version, change target framework, or change output paths globally (see "Shared build props" below).
- `BuildScripts/` — PowerShell scripts; **run them from inside `BuildScripts/`** (they `cd ../Projects/<name>` relatively).
- `bin/$(Configuration)/` — single repo-wide output directory for all non-MAUI projects (forced by `Localization.Import.csproj`, with `AppendTargetFrameworkToOutputPath=false`). MAUI projects override this to `bin/$(Configuration)/Maui/$(TargetFramework)/`.

## Build, pack, publish

```bash
# Build everything
dotnet build Solution/Armat.Localization/Armat.Localization.sln
dotnet build Solution/Armat.Localization/Armat.Localization.sln -c Release

# Build a single project (paths are relative to repo root)
dotnet build Projects/Localization.Core/Localization.Core.csproj
```

```powershell
# From inside BuildScripts/ — the scripts use relative `cd` and will fail elsewhere
cd BuildScripts
.\Pack.ps1                                 # all packable libs (Core, Wpf), Release
.\Pack.ps1 -Configuration Debug
.\Pack.ps1 -ProjectName Localization.Core  # single project
.\Publish.ps1                              # publishes Core, Wpf, Designer + zips them
```

There are **no test projects** in this solution; `dotnet test` is a no-op.

## Shared build props (`Projects/Localization.Import.csproj`)

Every csproj imports this. It centralizes:

- `Version` (single source of truth for assembly + NuGet version; bump it here, not in individual csprojs).
- `_DotNetVersion` — the **actual TFM in use is .NET 10** (`net10.0`), despite some Readmes still saying .NET 8. WPF/Designer projects extend it to `$(_DotNetVersion)-windows`; MAUI to `$(_DotNetVersion)-android` etc.
- `OutputPath = $(SolutionDir)\..\..\bin\$(Configuration)` and `AppendTargetFrameworkToOutputPath=false` — this is why builds land in the single shared `bin/<Config>/` regardless of TFM. MAUI csprojs override `OutputPath` to keep per-TFM directories (otherwise multi-TFM outputs would clobber each other).
- `Nullable=enable`, `ImplicitUsings=disable`, `EnforceCodeStyleInBuild=true`.

## Debug vs Release dependency wiring

Sub-projects (Wpf, Maui, Designer, Demo apps) reference Core/Wpf via **`ProjectReference` only when `$(Configuration) == 'Debug'`** and via **`PackageReference` to NuGet otherwise**:

```xml
<PackageReference Condition="'$(Configuration)' != 'Debug'" Include="armat.localization.core" Version="$(Version)$(_NugetVersionPostfix)" />
<ProjectReference Condition="'$(Configuration)' == 'Debug'" Include="..\Localization.Core\Localization.Core.csproj" />
```

Practical implications:
- Use **Debug** for inner-loop development — F12, breakpoints, edits in Core/Wpf flow into dependent projects.
- **Release builds resolve `armat.localization.core` / `.wpf` from NuGet at the version in `Localization.Import.csproj`.** If you bump `Version`, the package must be published before downstream Release builds will restore. To Release-build without publishing first, run `Pack.ps1` and add the local `bin/Release/pack/` directory as a NuGet source, or temporarily flip the conditions.

## Architecture: how localization works

The runtime model is the same across Core / WPF / MAUI; only the resource container differs.

**`LocalizationManager`** (Core) is a singleton-ish hub. `CreateDefaultInstance(...)` populates `LocalizationManager.Default` and **can only be called once** before anyone reads `Default`. It owns the `CurrentLocale`, fires `LocalizationChanged`, and holds a weakly-referenced `Targets` collection of `ILocalizationTarget` objects so disposed dictionaries clean up automatically.

**Localizable containers** all implement `ILocalizationTarget` + `ILocalizableResource` and follow the same lifecycle: load native content from a `Source` URI, register with a `LocalizationManager`, then on `OnLocalizationChanged` reload `.tsd`/`.trd` translations from `<TranslationsDirectory>/<localeName>/<file>.<ext>`.

| Container | Project | Native ext | Translation ext | Notes |
|---|---|---|---|---|
| `LocalizableStringDictionary` | Core | `.xaml` | `.tsd` | Plain `Dictionary<String,String>`, XML-serialized via `LocalizationDocument`. Works in any .NET app. |
| `LocalizableResourceDictionary` (WPF) | Wpf | `.xaml` | `.trd` | Subclass of WPF `ResourceDictionary`. Use `{DynamicResource}` so locale switches re-resolve. |
| `LocalizableResourceDictionary` (MAUI) | Maui | `.xaml` | `.trd` | Subclass of MAUI `ResourceDictionary`. **MAUI does NOT call `ISupportInitialize.EndInit()` on `ResourceDictionary` subclasses**, so initialization is hooked off the `IResourceDictionary.ValuesChanged` event instead — keep this if you touch init logic. |

**Translations directory layout** (resolved relative to `Configuration.TranslationsDirectoryPath`, typically `Localization/`):

```
Localization/
  <NativeFile>.xaml          # embedded resource or pack-uri
  en/<NativeFile>.tsd        # or .trd for WPF/MAUI resource dictionaries
  fr/<NativeFile>.tsd
  ...
```

`TranslationLoadBehavior` controls behavior for keys missing from a translation file: `KeepNative` (default), `ClearNative`, or `RemoveNative`.

## Designer app

`Localization.Designer` is a WPF tool that scans projects for `.xaml` files containing `LocalizableStringDictionary` or `LocalizableResourceDictionary` roots, lets the user add/remove locales, and writes the matching `.tsd`/`.trd` files into the correct `<locale>/` subdirectories. It detects file format via root element (see `LocalizableResourceFile` / `LocalizableResourceType`). Recent work added MAUI-resource-dictionary support; the `TranslationsDirRelativePath` parameter on resource dictionaries was removed because it was untested/broken — don't reintroduce it without verifying non-Windows targets.

## Wiring a localizable file in a consuming csproj

Native `.xaml` files **must** be embedded resources (Core/MAUI) or WPF `Resource`/`Page`, and translation `.tsd`/`.trd` files **must** be `CopyToOutputDirectory=PreserveNewest`. The existing `Demo/*` csprojs are the canonical templates — copy their `<ItemGroup>`s when adding new localizable files.

## Code style (from CONTRIBUTING.md)

- **Tabs** for indentation, **Allman** braces (open brace on its own line).
- Private fields: `_camelCase`. Locals/params: `camelCase`. Types/methods/props: `PascalCase`.
- Use BCL aliases — `String`, `Int32`, `Boolean` — not `string`/`int`/`bool`. Existing code is consistent on this.
