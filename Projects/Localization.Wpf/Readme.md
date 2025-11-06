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

- **armat.localization.core** (v2.0.1) - Core localization functionality
- **Microsoft.Extensions.Logging.Abstractions** (v9.0.9) - Logging support
- **.NET 8.0-windows** - Windows-specific .NET 8.0 framework with WPF support

See more details about the core functionality in [Armat.Localization.Core](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Core).

## `LocalizableResourceDictionary` class

Represents a specialized ResourceDictionary that extends WPF's native `ResourceDictionary` with localization capabilities. It implements `ILocalizationTarget` and `ILocalizableResource` interfaces to provide comprehensive translation management for WPF applications.

`LocalizableResourceDictionary` can be loaded from WPF assembly resources or local files, automatically managing translations based on the current locale selection.

### File Extensions

- **Native files**: `.xaml` - Standard XAML ResourceDictionary files
- **Translation files**: `.trd` (Translated Resource Dictionary) - Localized versions

### XAML Format

```xml
<l_wpf:LocalizableResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:System;assembly=netstandard"
                    xmlns:l_wpf="clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf">

    <s:String x:Key="MessageBox_Caption_Info">Information</s:String>
    <s:String x:Key="MessageBox_Caption_Warning">Warning</s:String>
    <s:String x:Key="MessageBox_Caption_Error">Error</s:String>
    
    <!-- Support for other resource types -->
    <SolidColorBrush x:Key="PrimaryBrush">#FF0078D4</SolidColorBrush>
    <Thickness x:Key="DefaultMargin">10</Thickness>

</l_wpf:LocalizableResourceDictionary>
```

### Key Properties and Methods

- **`LocalizationManager`** - Associates the dictionary with a localization manager for automatic language switching
- **`TranslationsDirRelativePath`** - Custom path for translation files (overrides default configuration)
- **`CurrentLocale`** - Gets the currently loaded locale information
- **`GetValueOrDefault<T>(key, defaultValue)`** - Type-safe resource retrieval with fallback support
- **`NativeFileExtensions`** / **`TranslationFileExtensions`** - Supported file extensions
- **Translation Management Methods**:
  - `LoadTranslation(locale)` - Loads translations for specified locale
  - `SaveTranslation()` - Saves current translations to file
  - `CreateTranslation(locale)` - Creates new translation file
  - `DeleteTranslation(locale)` - Removes translation file
  - `UpdateTranslations(translations)` - Batch update translations

### Constructors

```csharp
// Default constructor
var dictionary = new LocalizableResourceDictionary();

// With source URI
var dictionary = new LocalizableResourceDictionary("pack://application:,,,/MyApp;component/Resources/Strings.xaml");

// With custom localization manager
var dictionary = new LocalizableResourceDictionary(sourceUri, customLocalizationManager);
```


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
