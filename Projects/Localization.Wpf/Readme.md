# Armat Localization WPF

[![NuGet](https://img.shields.io/nuget/v/armat.localization.wpf.svg)](https://www.nuget.org/packages/armat.localization.wpf/)
[![Downloads](https://img.shields.io/nuget/dt/armat.localization.wpf.svg)](https://www.nuget.org/packages/armat.localization.wpf/)

The `Armat.Localization.Wpf` library extends the core localization functionality specifically for WPF applications. It provides specialized support for WPF Resource Dictionaries, enabling seamless localization of WPF user interfaces with runtime language switching.

## 🚀 Installation

Install via NuGet Package Manager:

```bash
dotnet add package armat.localization.wpf
```

Or via Package Manager Console:

```powershell
Install-Package armat.localization.wpf
```

## ✨ Features

- **WPF Resource Dictionary Support** - Native integration with WPF ResourceDictionary
- **Runtime UI Language Switching** - Change languages without application restart
- **XAML Integration** - Direct usage in WPF XAML files
- **Generic Value Retrieval** - Type-safe resource access with `GetValueOrDefault<T>`
- **Automatic Translation Loading** - Seamless translation file management
- **Comprehensive Logging** - Built-in logging support via Microsoft.Extensions.Logging.Abstractions
- **Thread-Safe Operations** - Safe for multi-threaded WPF applications

## Dependencies

- **armat.localization.core** - Core localization functionality
- **Microsoft.Extensions.Logging.Abstractions** - Logging support
- **.NET 8.0-windows** - Windows-specific .NET 8.0 framework with WPF support

See more details about the core functionality in [Armat.Localization.Core](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Core).

## `LocalizableResourceDictionary` class

Represents a specialized `ResourceDictionary` that extends WPF's native dictionary with localization capabilities. Implements `ISupportInitialize`, `ILocalizationTarget`, and `ILocalizableResource` to integrate with `LocalizationManager` instances, manage translation files, and react to locale changes.

`LocalizableResourceDictionary` can be instantiated from pack URIs or file paths. When attached to a `LocalizationManager`, it automatically reloads resources when the active locale changes.

### File Extensions

- **Native files**: `.xaml` – native resource dictionaries packaged with the application
- **Translation files**: `.trd` – translated resource dictionaries stored per locale

### Constructors

- `LocalizableResourceDictionary()` – creates an empty dictionary configured for localization
- `LocalizableResourceDictionary(String sourceUri)` – loads from a URI using the default localization manager
- `LocalizableResourceDictionary(String sourceUri, LocalizationManager locManager)` – loads from a URI using the supplied manager
- `LocalizableResourceDictionary(Uri source)` – loads from a `Uri` instance using the default manager
- `LocalizableResourceDictionary(Uri source, LocalizationManager locManager)` – loads from a `Uri` instance and registers with the supplied manager

### Properties

- `LocalizationManager` – assigns the manager that drives locale changes. The property can be set only once; setting it registers the dictionary as a target and creates a scoped logger.
- `TranslationsDirRelativePath` – optional override for the translations directory relative to the runtime directory.
- `CurrentLocale` – reports the locale currently applied to the dictionary.
- `NativeFileExtensions` / `TranslationFileExtensions` – arrays describing supported native (`xaml`) and translated (`trd`) file extensions. Static `NativeFileExtension` and `TranslationFileExtension` expose the individual extensions.
- `ResourceFilePath` – resolves the source URI to a file path or pack URI string, using `Source` or the XAML base URI.

### Methods

- `GetValueOrDefault<T>(String key, T defaultValue)` – retrieves a resource by key, returning the provided default when lookup or casting fails.
- `CanLoadNative(Uri sourceUri)` – validates that a file-based source contains a `LocalizableResourceDictionary` root element before loading.
- `LoadNative()` – reloads the native dictionary from the current `Source` and resets the loaded locale to native.
- `LoadNative(Uri sourceUri, LocalizationManager localizationManager)` – loads native content from the specified URI while registering with the given manager.
- `GetTranslationFilePath(LocaleInfo locale)` – composes the absolute path to the translation file for a locale, respecting `TranslationsDirRelativePath` and the manager configuration.
- `LoadTranslation(String localeName)` / `LoadTranslation(LocaleInfo locale)` – loads translations for the locale. Returns `false` when the locale is invalid or the translation file is missing, and applies `TranslationLoadBehavior` to unmatched keys.
- `SaveTranslation()` – persists the current contents to the locale-specific `.trd` file, ensuring WPF-compatible XML formatting.
- `CreateTranslation(LocaleInfo locale)` – creates an empty translation file and parent directories when they do not already exist.
- `DeleteTranslation(LocaleInfo locale)` – removes the translation file and deletes its directory when empty.
- `Enumerate()` – returns ordered `KeyValuePair<String, String>` entries for string resources within the dictionary.
- `UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)` – updates string resources for the active locale; throws when called for the native dictionary.

The class leverages `ISupportInitialize` to coordinate native loading and subsequent translation loading. Registration with `LocalizationManager` uses weak references (via the manager) so disposed dictionaries are cleaned up automatically. Translation file discovery relies on the manager's configured translations directory, and all operations are instrumented with `ILogger` for diagnostics.


## Usage Patterns

### Basic Setup

```csharp
// App.xaml.cs - Application startup
protected override void OnStartup(StartupEventArgs e)
{
    // Configure localization
    var config = new Configuration
    {
        DefaultLocale = new LocaleInfo("en"),
        TranslationsDirectoryPath = "Localization",
        TranslationLoadBehavior = TranslationLoadBehavior.KeepNative
    };
    
    // Create localization manager with logging
    using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var locManager = LocalizationManager.CreateDefaultInstance(config, loggerFactory);
    
    base.OnStartup(e);
}
```

### XAML Integration

```xml
<!-- MainWindow.xaml -->
<Window x:Class="MyApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <Window.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- Merge your localizable resource dictionary -->
                <LocalizableResourceDictionary Source="Resources/Strings.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Window.Resources>
    
    <Grid>
        <Button Content="{DynamicResource Button_OK_Text}" />
        <TextBlock Text="{DynamicResource Welcome_Message}" 
                   Foreground="{DynamicResource PrimaryBrush}" />
    </Grid>
</Window>
```

### Code-Behind Usage

```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    private LocalizableResourceDictionary _stringResources;
    
    public MainWindow()
    {
        InitializeComponent();
        
        // Access the merged resource dictionary
        _stringResources = (LocalizableResourceDictionary)Resources.MergedDictionaries[0];
        
        // Use type-safe resource access
        string welcomeMessage = _stringResources.GetValueOrDefault<string>("Welcome_Message", "Welcome!");
        MessageBox.Show(welcomeMessage);
    }
    
    private void OnLanguageChanged(object sender, RoutedEventArgs e)
    {
        // Change language at runtime
        LocalizationManager.Default.ChangeLocale("fr");
        // UI will automatically update through DynamicResource bindings
    }
}
```

### File Structure

```
MyWpfApp/
├── Resources/
│   └── Strings.xaml                 # Native resource dictionary
├── Localization/
│   ├── en/
│   │   └── Strings.trd             # English translations
│   ├── fr/
│   │   └── Strings.trd             # French translations
│   └── de/
│       └── Strings.trd             # German translations
└── bin/
    └── Debug/
        └── Localization/           # Copied translation files
            ├── en/
            ├── fr/
            └── de/
```

### Translation File Example

**Native File (Resources/Strings.xaml):**
```xml
<l_wpf:LocalizableResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:System;assembly=netstandard"
                    xmlns:l_wpf="clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf">

    <!-- String resources -->
    <s:String x:Key="Welcome_Message">Welcome to our application!</s:String>
    <s:String x:Key="Button_OK_Text">OK</s:String>
    <s:String x:Key="Button_Cancel_Text">Cancel</s:String>
    
    <!-- Other resource types -->
    <SolidColorBrush x:Key="PrimaryBrush">#FF0078D4</SolidColorBrush>
    <Thickness x:Key="DefaultMargin">10,5,10,5</Thickness>

</l_wpf:LocalizableResourceDictionary>
```

**Translation File (Localization/fr/Strings.trd):**
```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:System;assembly=netstandard">

    <s:String x:Key="Welcome_Message">Bienvenue dans notre application!</s:String>
    <s:String x:Key="Button_OK_Text">OK</s:String>
    <s:String x:Key="Button_Cancel_Text">Annuler</s:String>
    
    <!-- Resources can be culturally adapted -->
    <SolidColorBrush x:Key="PrimaryBrush">#FF0066CC</SolidColorBrush>
    <Thickness x:Key="DefaultMargin">8,4,8,4</Thickness>

</ResourceDictionary>
```

## Advanced Usage

### Custom Localization Targets

```csharp
// Custom control that responds to language changes
public class LocalizableUserControl : UserControl, ILocalizationTarget
{
    public LocaleInfo CurrentLocale { get; private set; } = LocaleInfo.Invalid;
    
    public LocalizableUserControl()
    {
        // Register with localization manager
        LocalizationManager.Default.Targets.Add(this);
    }
    
    public void OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)
    {
        CurrentLocale = args.NewLocale;
        UpdateLocalizedContent();
    }
    
    private void UpdateLocalizedContent()
    {
        // Update non-resource bound content
        // This is useful for dynamically generated content
    }
}
```

## Best Practices

1. **Use DynamicResource**: Always use `{DynamicResource}` instead of `{StaticResource}` for localizable content to enable runtime language switching
2. **Resource Types**: The WPF module supports all WPF resource types (strings, brushes, styles, etc.), not just strings
3. **Build Actions**: 
   - Set native .xaml files to "Resource" or "Page"
   - Set translation .trd files to "Copy to Output Directory: Copy if newer"
4. **Hierarchical Keys**: Use descriptive, hierarchical naming (e.g., `Dialog_Settings_Title`)
5. **Fallback Values**: Always provide meaningful default values with `GetValueOrDefault<T>`
6. **Threading**: The localization system is thread-safe, but UI updates must happen on the UI thread

### Use Localization.Designer

- Localization Designer source code is available [here](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Designer).
- Instead of manually creating translation files for each language, Localization Designer can be used to easily load & translate localizable files. It will create corresponding translation files in appropriate directories.
- Set "Copy to output directory" of generated translation files to "Copy if newer" in Visual Studio. Those will appear in the appropriate Localization subfolders in the bin directory.

## Demo Application

Complete usage examples are available through [Armat Localization Demo](https://github.com/ar-mat/Localization/tree/main/Projects/Demo) GitHub link.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
