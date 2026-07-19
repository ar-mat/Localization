# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution layout

The solution is **`Solution/Armat.Localization/Armat.Localization.sln`** — there is no .sln at the repo root. All `dotnet` commands targeting the whole solution must reference that path.

Top-level shape:

- `Projects/Localization.Core/` — runtime-agnostic core library (`armat.localization.core`).
- `Projects/Localization.Wpf/` — WPF `LocalizableResourceDictionary` (`armat.localization.wpf`, `net*-windows`, `UseWPF`).
- `Projects/Localization.Maui/` — MAUI `LocalizableResourceDictionary` (`armat.localization.maui`). TFMs are host-conditional: `-android` always, `-ios`/`-maccatalyst` on non-Linux hosts, `-windows10.0.19041.0` on Windows hosts only.
- `Projects/Localization.Designer/` — WPF + WinForms desktop translator GUI (`armat.localization.designer`). Now MAUI-aware.
- `Projects/Demo/{ClassLibrary,WpfApp,MauiApp}/` — usage examples wired into the .sln.
- `Projects/Localization.Import.csproj` — **shared MSBuild props imported by every project**. Not a buildable project. Edit this to bump version, change target framework, or change output paths globally (see "Shared build props" below).
- `Projects/Backup/` — stale manual backup of `Localization.Import.csproj` (still says 2.0.1 / net8.0). Not in the solution, not imported by anything — don't mistake it for the live file, don't update it.
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
.\Pack.ps1                                 # all packable libs (Core, Wpf, Maui), Release
.\Pack.ps1 -Configuration Debug
.\Pack.ps1 -ProjectName Localization.Core  # single project
.\Publish.ps1                              # publishes Core, Wpf, Maui, Designer + zips each
```

Pack output lands in `bin/<Config>/pack/<Project>/`, publish output in `bin/<Config>/publish/<Project>/` plus a versioned zip. `Publish.ps1` special-cases the MAUI library — it builds/publishes **only the Windows TFM** (`net10.0-windows10.0.19041.0`). `Pack.ps1` has no special case; the MAUI nupkg carries per-TFM `lib/` folders (`net10.0-android36.0`, `-ios26.0`, etc.).

There are **no test projects** in this solution; `dotnet test` is a no-op.

The MAUI library and demo require the MAUI workload installed: `dotnet workload install maui`.

### MAUI packaging & output quirks

- `Localization.Maui.csproj` and `Demo/MauiApp.csproj` **pin `TargetPlatformVersion`** (iOS/Mac Catalyst `26.0`, Android `36.0`) so the packed NuGet `lib/` folders are deterministic. Without the pins, whatever platform SDK the packing machine happens to have silently ratchets the package TFMs upward and locks out consumers on older SDKs (NU1202). Don't remove them; lower them only alongside explicitly-versioned workload installs.
- Both MAUI csprojs set `SatelliteResourceLanguages=en` to suppress per-locale `Microsoft.Maui.Controls.resources.dll` satellite folders in the output — framework UI strings, unrelated to this library's `Localization/<locale>/` files.
- `Demo/MauiApp.csproj` additionally has a `RemoveWinUIMuiLocaleFolders` post-build target: WinUI ships native `.mui` resources in per-language folders that `SatelliteResourceLanguages` can't filter (they aren't managed satellites), so the target deletes output subdirectories whose names match the IETF language-tag shape, keeping only `en-us`.

## Shared build props (`Projects/Localization.Import.csproj`)

Every csproj imports this. It centralizes:

- `_ArmatLocalizationVersion` — single source of truth for versioning (currently `3.0.0`); bump it here, not in individual csprojs. `Version`, `AssemblyVersion`, and `FileVersion` derive from it. `_NugetVersionPostfix` (currently `-beta`) is appended to form each library's `PackageVersion` and the `PackageReference` versions in the Debug/Release wiring — so assemblies are `3.0.0` while NuGet packages resolve as `3.0.0-beta`. Clear the postfix for a stable release.
- `_ReleaseNotes` points at the root `ReleaseNotes.md` on GitHub — maintain the top section there for the upcoming release and stamp the date when publishing. It's a plain variable, not `PackageReleaseNotes` directly, because this file is imported by every project including non-packable ones (Designer, Demos); each packable csproj (Core/Wpf/Maui) opts in with its own `<PackageReleaseNotes>$(_ReleaseNotes)</PackageReleaseNotes>`.
- `_DotNetVersion` — the **actual TFM is .NET 10** (`net10.0`). WPF/Designer projects extend it to `$(_DotNetVersion)-windows`; MAUI to `$(_DotNetVersion)-android`, `-ios`, `-maccatalyst`, and (on Windows hosts) `-windows10.0.19041.0`.
- `OutputPath = $(SolutionDir)\..\..\bin\$(Configuration)` and `AppendTargetFrameworkToOutputPath=false` — this is why builds land in the single shared `bin/<Config>/` regardless of TFM. MAUI csprojs override `OutputPath` to keep per-TFM directories (otherwise multi-TFM outputs would clobber each other). `obj/` is centralized too: `IntermediateOutputPath` goes under `bin/<Config>/obj/<Project>/`.
- `Nullable=enable`, `ImplicitUsings=disable`, `EnforceCodeStyleInBuild=true`.

## Debug vs Release dependency wiring

Sub-projects (Wpf, Maui, Designer, Demo apps) reference Core/Wpf via **`ProjectReference` only when `$(Configuration) == 'Debug'`** and via **`PackageReference` to NuGet otherwise**:

```xml
<PackageReference Condition="'$(Configuration)' != 'Debug'" Include="armat.localization.core" Version="$(_ArmatLocalizationVersion)$(_NugetVersionPostfix)" />
<ProjectReference Condition="'$(Configuration)' == 'Debug'" Include="..\Localization.Core\Localization.Core.csproj" />
```

Practical implications:
- Use **Debug** for inner-loop development — F12, breakpoints, edits in Core/Wpf flow into dependent projects.
- **Release builds resolve `armat.localization.core` / `.wpf` / `.maui` from NuGet at `$(_ArmatLocalizationVersion)$(_NugetVersionPostfix)`** (e.g. `3.0.0-beta`). If you bump the version, the packages must be published before downstream Release builds will restore. To Release-build without publishing first, run `Pack.ps1` and add the local `bin/Release/pack/` directory as a NuGet source, or temporarily flip the conditions.

## Architecture: how localization works

The runtime model is the same across Core / WPF / MAUI; only the resource container differs.

**`LocalizationManager`** (Core) is a singleton-ish hub. `CreateDefaultInstance(...)` populates `LocalizationManager.Default` and throws if called twice. Reading `Default` before it is called returns a placeholder manager; targets registered with the placeholder never migrate — create the default manager before constructing any localizable resources (a warning is logged otherwise). Locale changes and target registration are expected to happen on a single (UI) thread. It owns the `CurrentLocale`, fires `LocalizationChanged`, and holds a weakly-referenced `Targets` collection of `ILocalizationTarget` objects so disposed dictionaries clean up automatically.

**Localizable containers** all implement `ILocalizationTarget` + `ILocalizableResource` and follow the same lifecycle: load native content from a `Source` URI, register with a `LocalizationManager`, then on `OnLocalizationChanged` reload `.tsd`/`.trd` translations.

| Container | Project | Native ext | Translation ext | Notes |
|---|---|---|---|---|
| `LocalizableStringDictionary` | Core | `.xaml` | `.tsd` | Plain `Dictionary<String,String>`, XML-serialized via `LocalizationDocument`. Works in any .NET app. |
| `LocalizableResourceDictionary` (WPF) | Wpf | `.xaml` | `.trd` | Subclass of WPF `ResourceDictionary`. Use `{DynamicResource}` so locale switches re-resolve. |
| `LocalizableResourceDictionary` (MAUI) | Maui | `.xaml` | `.trd` | Subclass of MAUI `ResourceDictionary`. **MAUI does NOT call `ISupportInitialize.EndInit()` on `ResourceDictionary` subclasses**, so initialization is hooked off the `IResourceDictionary.ValuesChanged` event instead. The handler also early-returns when `Source == null` so XAML-instantiated dictionaries don't run init prematurely. |

**Translations directory layout** (resolved relative to `Configuration.TranslationsDirectoryPath`, typically `Localization/`):

```
Localization/
  <NativeFile>.xaml          # embedded resource or pack-uri
  en/<NativeFile>.tsd        # or .trd for WPF/MAUI resource dictionaries
  fr/<NativeFile>.tsd
  ...
```

`TranslationLoadBehavior` controls behavior for keys missing from a successfully loaded translation file: `KeepNative` (default), `ClearNative`, or `RemoveNative`. Empty-string values in a translation file are treated the same as missing keys (the Designer persists untranslated cells as empty strings, so this is what makes partial translations fall back to native at runtime). A failed `LoadTranslation` (invalid locale or missing file) returns `false` and leaves the dictionary contents and `CurrentLocale` fully unchanged — the load behavior is not applied in that case. For absolute file `Source`s only the file name is kept — the translation resolves to `<TranslationsDir>/<locale>/<file name>`.

### MAUI translation file resolution

The MAUI dictionary tries two sources in order during `LoadTranslation`:

1. **App package asset** via `Microsoft.Maui.Storage.FileSystem.OpenAppPackageFileAsync(assetPath)`. This is what works on Android / iOS / Mac Catalyst, where the file system isn't directly accessible. The asset path is forward-slashed and **must** be prefixed with `Configuration.TranslationsDirectoryPath` — translation files have to be wired with `<MauiAsset Include="Localization\**\*.trd" LogicalName="Localization\%(RecursiveDir)%(Filename)%(Extension)" />` in the consuming MAUI csproj for that lookup to succeed.
2. **File system** via the absolute path computed by `GetTranslationFilePath`. This is the Windows fall-back and the only path the Designer relies on.

When `Configuration.SupportedLocales` isn't set, `LocalizationManager.AllLocales` falls back to scanning the translations directory — that scan can't see `MauiAsset`-packaged files at runtime, so MAUI apps almost always need to set `Configuration.SupportedLocales` explicitly (see `Projects/Demo/MauiApp/App.xaml.cs`).

MAUI forbids setting `ResourceDictionary.Source` from code ("Source can only be set from XAML"), so programmatic loading is unsupported on MAUI: the `Uri`-taking constructors fail at runtime with MAUI's own `InvalidOperationException` (deliberately kept in case MAUI permits programmatic sources later), and `LoadNative(Uri, LocalizationManager)` throws `NotSupportedException`. Switching back to the native locale (`LoadNative()`) restores values by reading MAUI's internal `_mergedInstance` field via reflection: when a dictionary is loaded from `Source`, MAUI keeps the native XAML contents in that pristine merged instance while translations are applied as overrides on the outer dictionary — so the merged instance is the only reliable source for restoring native values (the outer dictionary's own enumerator yields only the overrides, never the merged native entries). MAUI `Source` URIs use the `Path.xaml;assembly=AssemblyName` form — path first, unlike WPF's `/Assembly;component/Path` — which the translation-path helpers account for by keeping the substring before `';'`.

### Configuration notes

- `Configuration` is a **mutable record struct** (`set`, not `init`) with a `SupportedLocales` property (`IEnumerable<LocaleInfo>?`). When non-null, `LocalizationManager.AllLocales` returns it directly instead of scanning the translations directory (as-is — the `DefaultLocale` prepend applies only to the directory-scan fallback).
- `ILocalizableResource.Source` is `Uri?` (nullable). Implementations may return null before `LoadNative` has been called.
- `LocaleInfo` equality is by culture `Name` only — `DisplayNameOverride` is cosmetic and doesn't participate in comparisons.

## Designer app

`Localization.Designer` is a WPF tool that scans projects for `.xaml` files containing one of three root-element flavours:

| Root element + namespace | Resource type |
|---|---|
| `LocalizableStringDictionary` | `LocalizableResourceType.StringDictionary` |
| `LocalizableResourceDictionary` with `xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"` | `WpfResourceDictionary` |
| `LocalizableResourceDictionary` with `xmlns="http://schemas.microsoft.com/dotnet/2021/maui"` | `MauiResourceDictionary` |

The MAUI branch is implemented by `LocalizableMauiResourceDictionary` (Designer-internal). It wraps a `Wpf.LocalizableResourceDictionary` as `_innerDictionary` and:

- On load: copies the source MAUI XAML into a temp file, swaps MAUI ↔ WPF XML namespaces (default ns, `xmlns:x`, library clr-namespace), strips the root `x:Class` attribute (regex-based, byte-preserving), then hands the temp file to `_innerDictionary.LoadNative`.
- On translation load: reads the actual MAUI `.trd` (path computed locally because `_innerDictionary.Source` points at the temp file), namespace-swaps into a temp WPF file, parses with `XamlReader.Load`, merges into `_innerDictionary`.
- On save / create: serializes `_innerDictionary` (or an empty dictionary) to a temp WPF XAML, swaps WPF → MAUI namespaces, writes the result to the target `.trd` path. The conversion is plain `String.Replace` of the three differing namespace URIs — longer/more-specific replaced first to avoid `xaml/presentation` being corrupted by the substring `xaml`.

The `TranslationsDirRelativePath` parameter on resource dictionaries was removed earlier because it was untested/broken — don't reintroduce it without verifying non-Windows targets.

`LocalizableResourceFile.Load(String resourceFilePath, String? translationsDirectoryPath = null)` takes an optional explicit translations directory. When omitted, it defaults to `Path.GetDirectoryName(FullPath)`. `MainWindow.DetectTranslationsDirectoryPath(filePath, rootDirectoryPath)` populates that argument by walking up the directory tree:

1. If `rootDirectoryPath` is null, locate the nearest ancestor containing a `*.csproj` file.
2. Walk up again from `filePath` looking for an ancestor named `Localization` (constant pulled from `Configuration.Default.TranslationsDirectoryPath`), constrained to the resolved `rootDirectoryPath`. The constraint helper is **strict-descendant** as currently written — `dir == root` returns false. Fine in practice (project roots aren't named `Localization`) but worth knowing.

## Wiring a localizable file in a consuming csproj

Native `.xaml` files **must** be embedded resources (Core / MAUI) or WPF `Resource`/`Page`, and translation `.tsd`/`.trd` files **must** be `CopyToOutputDirectory=PreserveNewest`. MAUI translation files additionally need `<MauiAsset Include="…" LogicalName="…" />` so they're packaged into the app bundle on Android / iOS / Mac Catalyst. The existing `Demo/*` csprojs are the canonical templates — copy their `<ItemGroup>`s when adding new localizable files.

## Code style (full guide in `CONTRIBUTING.md`)

- **Tabs** for indentation, **Allman** braces (open brace on its own line).
- Private fields: `_camelCase`. Locals/params: `camelCase`. Types/methods/props: `PascalCase`.
- Use BCL aliases — `String`, `Int32`, `Boolean`, `Object` — not `string`/`int`/`bool`. The codebase is fully consistent on this.
- Comments are **plain `//` line comments**, lowercase first letter for in-method step descriptions, used for section headers above grouped members and for explaining *why* a non-obvious decision was made. XML doc comments are reserved for the auto-generated WPF code-behind partials (`/// Interaction logic for X.xaml`).
- File-scoped namespaces, nullable reference types enabled.
- Commit messages use the `type(scope):` form — `feat(core): …`, `fix(maui): …`, `docs(designer): …`. All contributions go through PRs to `main` (no direct commits).
- CONTRIBUTING.md asks that per-project `Readme.md` files and this file be updated whenever a change alters public API, file layout, or build behavior.
