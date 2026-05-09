# Armat Localization MAUI

[![NuGet](https://img.shields.io/nuget/v/armat.localization.maui.svg)](https://www.nuget.org/packages/armat.localization.maui/)
[![Downloads](https://img.shields.io/nuget/dt/armat.localization.maui.svg)](https://www.nuget.org/packages/armat.localization.maui/)

The `Armat.Localization.Maui` library extends the core localization functionality for .NET MAUI applications. It plugs a `LocalizableResourceDictionary` into the MAUI XAML pipeline so any MAUI page or control can pull translated strings, brushes, styles, etc. through the standard `{DynamicResource}` markup, and switch language at runtime without restarting the app.

## 🚀 Installation

Install via NuGet Package Manager:

```bash
dotnet add package armat.localization.maui
```

Or via Package Manager Console:

```powershell
Install-Package armat.localization.maui
```

## ✨ Features

- **MAUI ResourceDictionary integration** — drop-in subclass of `Microsoft.Maui.Controls.ResourceDictionary`.
- **Cross-platform asset loading** — translations read via `Microsoft.Maui.Storage.FileSystem` on Android / iOS / Mac Catalyst (packaged as `MauiAsset`), with automatic fall-back to the file system on Windows.
- **Runtime UI language switching** — locale changes propagate through `LocalizationManager` and re-resolve every `{DynamicResource}` binding.
- **XAML-first usage** — declare `<lm:LocalizableResourceDictionary Source="…" />` in any MAUI XAML and you're done.
- **Type-safe resource access** — `GetValueOrDefault<T>(key, defaultValue)` for code-behind lookups.
- **Same translation files as WPF** — `.xaml` for natives, `.trd` for translations; the Localization Designer recognizes both formats.

## Dependencies

- **armat.localization.core** — core localization functionality.
- **Microsoft.Maui.Controls** — MAUI runtime.
- **Microsoft.Extensions.Logging.Abstractions** — diagnostics.
- **.NET 10** — multi-targets `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, and (on Windows) `net10.0-windows10.0.19041.0`.

See the core API in [Armat.Localization.Core](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Core).

## `LocalizableResourceDictionary` class

Specialized `Microsoft.Maui.Controls.ResourceDictionary` that implements `ILocalizationTarget` and `ILocalizableResource`. When attached to a `LocalizationManager`, it reloads its contents whenever the active locale changes.

### MAUI initialization quirk

Unlike WPF, **MAUI does not call `ISupportInitialize.EndInit()` on `ResourceDictionary` subclasses**. This library subscribes to `IResourceDictionary.ValuesChanged` to detect when MAUI has finished injecting the native XAML contents, then runs the post-init logic exactly once. Don't rely on `EndInit()` being invoked from MAUI XAML.

### File Extensions

- **Native files**: `.xaml` — native resource dictionaries packaged with the application.
- **Translation files**: `.trd` — translated resource dictionaries stored per-locale under `Localization/<localeName>/`.

### Constructors

- `LocalizableResourceDictionary()` — empty dictionary; suitable for XAML instantiation.
- `LocalizableResourceDictionary(String sourceUri)` — loads from a URI using the default localization manager.
- `LocalizableResourceDictionary(String sourceUri, LocalizationManager locManager)` — loads from a URI using the supplied manager.
- `LocalizableResourceDictionary(Uri source)` — loads from a `Uri` using the default manager.
- `LocalizableResourceDictionary(Uri source, LocalizationManager locManager)` — loads from a `Uri` and registers with the supplied manager.

### Properties

- `LocalizationManager` — assigns the manager that drives locale changes. The property can be set only once; setting it registers the dictionary as a target and creates a scoped logger.
- `CurrentLocale` — the locale currently applied to the dictionary.
- `NativeFileExtensions` / `TranslationFileExtensions` — `xaml` and `trd` arrays. Static `NativeFileExtension` and `TranslationFileExtension` expose the individual extensions.
- `ResourceFilePath` — resolves the source URI to a file path or pack-style string.

### Methods

- `GetValueOrDefault<T>(String key, T defaultValue)` — resource lookup that returns the default on missing keys or cast failures.
- `CanLoadNative(Uri sourceUri)` — checks the file's root element name and confirms `xmlns="http://schemas.microsoft.com/dotnet/2021/maui"` before accepting it as a MAUI native dictionary.
- `LoadNative()` — reloads from the current `Source`.
- `LoadNative(Uri sourceUri, LocalizationManager localizationManager)` — loads native XAML from a URI and registers with the given manager.
- `GetTranslationAssetPath(LocaleInfo locale)` — composes the relative asset path used by `FileSystem.OpenAppPackageFileAsync` (forward-slash, `Localization/<locale>/<file>.trd`).
- `GetTranslationFilePath(LocaleInfo locale)` — composes the absolute file system path used as the Windows fall-back.
- `LoadTranslation(String localeName)` / `LoadTranslation(LocaleInfo locale)` — loads a translation. Tries the MAUI app package asset first; falls back to the file system. Returns `false` when the locale is invalid or no source can be located, and applies `TranslationLoadBehavior` to keys missing from the translation.
- `SaveTranslation()` — writes the current contents to the locale-specific `.trd` file (Windows / Designer scenario; MAUI runtime apps generally don't write back from the device).
- `CreateTranslation(LocaleInfo locale)` — creates an empty translation file and parent directories.
- `DeleteTranslation(LocaleInfo locale)` — removes the translation file and the locale directory if empty.
- `Enumerate()` — ordered `KeyValuePair<String, String>` view of string resources.
- `UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)` — updates string resources for the active locale; throws when called for the native dictionary.

Translation discovery is rooted at `LocalizationManager.Configuration.TranslationsDirectoryPath`. Logging goes through `ILogger`. Registration with the manager uses weak references so disposed dictionaries are cleaned up automatically.

## Usage Patterns

### Application setup

`LocalizationManager` is normally created from your `App` constructor (rather than `MauiProgram.CreateMauiApp`) so it's ready before the first page resolves resources:

```csharp
// App.xaml.cs
public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        Configuration config = Configuration.Default with { DefaultLocale = new LocaleInfo("en") };
        config.SupportedLocales = new[] { new LocaleInfo("en"), new LocaleInfo("hy"), new LocaleInfo("ru") };

        LocalizationManager.CreateDefaultInstance(config);
    }

    protected override Window CreateWindow(IActivationState? activationState) =>
        new Window(new AppShell());
}
```

### XAML integration

```xml
<!-- MainPage.xaml -->
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="MyApp.MainPage"
             xmlns:lm="clr-namespace:Armat.Localization.Maui;assembly=armat.localization.maui">

    <ContentPage.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <lm:LocalizableResourceDictionary Source="/Localization/StringTable.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </ContentPage.Resources>

    <VerticalStackLayout Padding="30,0" Spacing="25">
        <Label Text="{DynamicResource Lbl_HelloWorld}" />
        <Button Text="{DynamicResource Btn_ClickMe}"
                SemanticProperties.Hint="{DynamicResource Btn_ClickMe_Hint}"
                Clicked="OnCounterClicked" />
    </VerticalStackLayout>
</ContentPage>
```

Notice: use **`{DynamicResource}`**, not `{StaticResource}` — only dynamic references re-resolve when the locale flips.

### Code-behind language switching

```csharp
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
    }

    private void OnSwitchToFrench(Object sender, EventArgs e)
    {
        // every LocalizableResourceDictionary registered with the manager will reload
        LocalizationManager.Default.ChangeLocale("fr");
    }
}
```

### Project file (.csproj) wiring

Native `.xaml` files are compiled by the MAUI XAML pipeline. Translation `.trd` files must ship as `MauiAsset` so they're packaged into the app bundle on Android / iOS / Mac Catalyst, and as `CopyToOutputDirectory` so the Windows fall-back finds them on disk:

```xml
<ItemGroup>
    <None Update="Localization\**\*.trd">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
        <TargetPath>%(RecursiveDir)%(Filename)%(Extension)</TargetPath>
    </None>
    <MauiAsset Include="Localization\**\*.trd"
               LogicalName="Localization\%(RecursiveDir)%(Filename)%(Extension)" />

    <MauiXaml Update="Localization\StringTable.xaml">
        <Generator>MSBuild:Compile</Generator>
    </MauiXaml>
</ItemGroup>
```

The `LogicalName` for `MauiAsset` **must** start with the same `TranslationsDirectoryPath` configured on `LocalizationManager` (default: `Localization`) — that's the prefix `GetTranslationAssetPath` looks for when calling `FileSystem.OpenAppPackageFileAsync`.

### File structure

```
MyMauiApp/
├── Localization/
│   ├── StringTable.xaml             # Native MAUI resource dictionary
│   ├── en/
│   │   └── StringTable.trd          # English translations
│   ├── hy/
│   │   └── StringTable.trd
│   └── ru/
│       └── StringTable.trd
├── MainPage.xaml
├── App.xaml
└── MauiApp.csproj
```

### Native file format

```xml
<?xml version="1.0" encoding="utf-8" ?>
<lm:LocalizableResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                                  x:Class="MyApp.Localization.StringTable"
                                  xmlns:lm="clr-namespace:Armat.Localization.Maui;assembly=armat.localization.maui"
                                  xmlns:s="clr-namespace:System;assembly=netstandard">

    <s:String x:Key="Lbl_HelloWorld">Hello, world!</s:String>
    <s:String x:Key="Btn_ClickMe">Click me</s:String>
    <s:String x:Key="Btn_ClickMe_Hint">Counts the number of times you click</s:String>

</lm:LocalizableResourceDictionary>
```

### Translation file format (`Localization/fr/StringTable.trd`)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<lm:LocalizableResourceDictionary xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
                                  xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
                                  xmlns:lm="clr-namespace:Armat.Localization.Maui;assembly=armat.localization.maui"
                                  xmlns:s="clr-namespace:System;assembly=netstandard">

    <s:String x:Key="Lbl_HelloWorld">Bonjour le monde !</s:String>
    <s:String x:Key="Btn_ClickMe">Cliquez-moi</s:String>
    <s:String x:Key="Btn_ClickMe_Hint">Compte le nombre de clics</s:String>

</lm:LocalizableResourceDictionary>
```

`SaveTranslation()` writes a minimal form of this file (root `ResourceDictionary` in the MAUI namespace plus `<x:String x:Key="…">…</x:String>` children); the additional `xmlns:lm` declaration is only needed if you'll later edit the file as a native dictionary.

## Best Practices

1. **Use `{DynamicResource}`** for any localizable property — `{StaticResource}` won't refresh on locale change.
2. **Create `LocalizationManager.Default` from `App.xaml.cs`** — before the first page is built. Doing it in `MauiProgram` is too late for the initial XAML resolution pass.
3. **Set `Configuration.SupportedLocales`** when your translations live as `MauiAsset` — the directory-scanning fallback in `LocalizationManager.AllLocales` cannot enumerate app package assets at runtime.
4. **Keep the asset `LogicalName` aligned with `TranslationsDirectoryPath`** — mismatches make `OpenAppPackageFileAsync` silently miss the file and the manager falls back to the file system (which doesn't exist on mobile devices).
5. **Hierarchical keys** (`Page_Settings_Title`, `Btn_Save_Text`) make translations easier to maintain.
6. **Threading** — the manager is thread-safe, but UI updates must go through the MAUI dispatcher.

## Localization.Designer support

The Localization Designer (a Windows-only WPF tool) recognizes MAUI `LocalizableResourceDictionary` files via their root element name and the `http://schemas.microsoft.com/dotnet/2021/maui` default namespace, and can edit / save matching `.trd` files in the appropriate `<locale>/` subdirectories. See the [Designer documentation](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Designer).

## Demo Application

A complete MAUI demo lives at [`Projects/Demo/MauiApp`](https://github.com/ar-mat/Localization/tree/main/Projects/Demo/MauiApp).

## Contributing

Contributions are welcome. For major changes, open an issue first to discuss the design.

## License

This project is licensed under the MIT License — see the [LICENSE](../../LICENSE.txt) file for details.
