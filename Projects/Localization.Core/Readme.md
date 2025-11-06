# Armat Localization Core

[![NuGet](https://img.shields.io/nuget/v/armat.localization.core.svg)](https://www.nuget.org/packages/armat.localization.core/)
[![Downloads](https://img.shields.io/nuget/dt/armat.localization.core.svg)](https://www.nuget.org/packages/armat.localization.core/)

The `Armat.Localization.Core` library provides the foundational functionality for localizing .NET applications. This is the core module that enables simple text localization for any .NET application type, with support for multiple languages and runtime language switching.

## 🚀 Installation

Install via NuGet Package Manager:

```bash
dotnet add package armat.localization.core
```

Or via Package Manager Console:

```powershell
Install-Package armat.localization.core
```

## ✨ Features

- **Universal .NET support** - Works with any .NET application (.NET 8.0+)
- **Runtime language switching** - Change languages without application restart
- **Comprehensive locale management** - Built on `System.Globalization.CultureInfo`
- **Flexible resource loading** - Support for embedded resources and external files
- **Thread-safe operations** - Safe for multi-threaded applications
- **Minimal dependencies** - Only requires `Microsoft.Extensions.Logging.Abstractions`
- **Extensible architecture** - Base for specialized libraries (WPF, etc.)
- **Weak reference management** - Automatic cleanup of disposed localization targets

Note: This is the core module of `Armat.Localization` library. It can be used for localizing strings for all types of .Net applications.
Below are derived libraries for specialized application types:

Armat.Localization.Wpf can be used for localizing Wpf Resource Dictionaries. See [here](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Wpf) for more information.


## Main components

The root namespace for the class library is `Armat.Localization`.
All types described below belong to `Armat.Localization` namespace.

### `LocaleInfo` record class

Represents a wrapper over `System.Globalization.CultureInfo` class, specializing it for `Armat.Localization.Core` library.

- `Invalid` static property returns a singleton instance of `LocaleInfo` class to be used for *null* or *[Native]* locale.
- `AllLocales` is static property for enumerating all locales. It's based on `System.Globalization.CultureTypes.AllCultures`, and provides comprehensive list of locales supported by the library.
- `Culture` nullable property provides the underlying instance of `System.Globalization.CultureInfo` class.
- `IsValid` property can be used to check whether the current instance of `LocaleInfo` is a valid instance (`Culture != null`).
- `Name` property can be used for locale identification. It's not localizable. `Name` returns the value of wrapped `CultureInfo.Name` for non-invalid locales, and is an empty string otherwise.
- `DisplayName` property can be used for displaying the locale on the UI. It changes based on the `Thread.CurrentThread.CurrentUICulture`. It has the same value as the wrapped `CultureInfo.DisplayName`, although could be overridden at `LocaleInfo` construction time.

### `LocalizationManager` class

Acts as a central component of `Armat.Localization.Core` library.

- `CreateDefaultInstance` static method can be used at application startup to instantiate the default singleton instance of `LocalizationManager` class. It has overloads to accept `Configuration` parameter and `ILoggerFactory` for logging support, and can be called only once before any usage of `LocalizationManager.Default`. If not created explicitly, the one with default configuration will be created automatically.
- `CreateInstance` static method creates a new non-singleton instance of `LocalizationManager` with provided configuration and logger factory.
- `Default` static property representing the singleton instance of `LocalizationManager` class to be used across the application.
- `Configuration` property describes the Localization Manager configuration of `Armat.Localization.Configuration` type.
- `AllLocales` property enumerates collection of `LocaleInfo` objects. These are the locales that application has translations for, determined by scanning the translations directory structure.
- `CurrentLocale` read-only property returns the currently selected `LocaleInfo` for the application.
- `ChangeLocale` method allows switching between supported locales at application runtime. There are overloads accepting either `LocaleInfo` or the locale name `String` parameters.
- `Targets` collection property manages `ILocalizationTarget` objects that receive locale change notifications.
- `LocalizationChanged` event is fired when the locale changes, allowing external components to respond to language switches.

Note: Considering there could be more than one instance of `LocalizationManager` objects, invocation of `ChangeLocale` method doesn't change values of `Thread.CurrentThread.CurrentCulture` or `Thread.CurrentThread.CurrentUICulture` static properties, considering application will change its thread(s) culture(s) based on the appropriate language change callback event.

Note: Passing *LocaleInfo.Invalid* locale to `LocalizationManager.ChangeLocale` will reset application language to the native locale.

The class uses weak references to manage localization targets, ensuring automatic cleanup when targets are disposed.

### `Configuration` record struct

Describes configuration parameters for `LocalizationManager` class provided at construction time.

- `DefaultLocale` is a nullable `LocaleInfo` field referring to the locale used at application startup. In case of *null* value the *[Native]* locale will be used.
- `TranslationsDirectoryPath` points to the absolute or relative path to localizable resources translation directory. Default value is "Localization".
- `TranslationLoadBehavior` is an enumeration of `TranslationLoadBehavior` type with possible values of `KeepNative` (default value), `ClearNative` and `RemoveNative`.

The `Configuration.Default` static property provides default configuration values that can be used for most applications.

Note: `LocalizationManager.AllLocales` will return a special *[Native]* locale additional to the other locales if *Configuration.DefaultLocale == LocaleInfo.Invalid*. This is the default behavior.

### `LocalizableStringDictionary` class

Represents a type derived from `Dictionary<String, String>` of key-value pairs. The dictionary key (aka resource key) is used to retrieve appropriate localized values.

`LocalizableStringDictionary` can be loaded from an assembly embedded resource, as well as from a local file. The class is used for retrieving translated strings at application runtime and implements both `ILocalizationTarget` and `ILocalizableResource` interfaces.

Following is the format for `LocalizableStringDictionary` resource files:

```xml
<LocalizableStringDictionary>
	<String Key="MessageBox_Caption_Info" Value="Information"/>
	<String Key="MessageBox_Caption_Warning" Value="Warning"/>
	<String Key="MessageBox_Caption_Error" Value="Error"/>
</LocalizableStringDictionary>
```

`LocalizableStringDictionary` class has the following members:
- `LocalizationManager` property can be set to attach the string dictionary instance to the localization manager. Upon locale change in the localization manager, the appropriate translation will be loaded in the `LocalizableStringDictionary`.
- `Source` property is used to load the native resource file at a given `Uri`. `Uri` formatting is done in the following way:
	- For embedded resource files the `Source` Uri must be constructed with uriString: `{AssemblyName};component/{ManifestResourceStreamPath}` and uriKind: `UriKind.Relative`.
	- For local file paths `Source` Uri must be constructed with uriString: `path to the localizable file` and uriKind: `UriKind.Absolute`.
- `TranslationsDirRelativePath` property holds the path to the translations directory in case *LocalizationManager.Configuration.TranslationsDirectoryPath* should be overridden.
- `CurrentLocale` property represents the `LocaleInfo` currently loaded in the string dictionary.
- `GetValueOrDefault` method can be used to retrieve the translated value from dictionary if available, or the given default value otherwise. This is the API to be used at runtime to read localized values for the given key.
- `NativeFileExtensions` and `TranslationFileExtensions` properties define supported file extensions ("xaml" for native files, "tsd" for translation files).
- `FormResourceUri` static method helps create proper URI from a Type for embedded resources.

The class implements `ISupportInitialize` interface for proper initialization sequencing and provides comprehensive translation file management including creation, loading, saving, and deletion of translation files.


## Additional Interfaces and Types

### `ILocalizationTarget` Interface

Defines the contract for objects that need to receive locale change notifications:
- `CurrentLocale` property - Gets the current locale of the target
- `OnLocalizationChanged` method - Called when the locale changes

### `ILocalizableResource` Interface  

Defines the contract for localizable resource management:
- `NativeFileExtensions` and `TranslationFileExtensions` properties - Supported file extensions
- `Source` property - URI of the resource source
- `LoadNative` method - Loads native language content
- `LoadTranslation`, `SaveTranslation`, `CreateTranslation`, `DeleteTranslation` methods - Translation file management
- `Enumerate` method - Returns key-value pairs for the current locale
- `UpdateTranslations` method - Updates translations with new values

### `LocalizationChangeEventArgs` Class

Event arguments for locale change events:
- `OldLocale` property - Previous locale (nullable)
- `NewLocale` property - New locale

### `TranslationLoadBehavior` Enumeration

Defines how missing translations are handled:
- `KeepNative` - Keep original native values (default)
- `ClearNative` - Replace with empty strings
- `RemoveNative` - Remove keys entirely


## General Usage Pattern

The usage pattern will be demonstrated on a sample of WPF application.
It's available through [Armat Localization Demo](https://github.com/ar-mat/Localization/tree/main/Projects/Demo) GitHub link.

### Create LocalizationManager

```csharp
// Create configuration
var config = new Configuration
{
    DefaultLocale = new LocaleInfo("en"), // or LocaleInfo.Invalid for native
    TranslationsDirectoryPath = "Localization", // relative or absolute path
    TranslationLoadBehavior = TranslationLoadBehavior.KeepNative
};

// Create logger factory (optional)
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

// Create the default LocalizationManager instance
var lm = LocalizationManager.CreateDefaultInstance(config, loggerFactory);

// Change locale at runtime
lm.ChangeLocale("fr"); // Switch to French

// Register for locale change events
lm.LocalizationChanged += (sender, args) => {
    Console.WriteLine($"Locale changed from {args.OldLocale?.Name} to {args.NewLocale.Name}");
};
```

### Create LocalizableStringDictionary

```csharp
// For embedded resources
var resourceUri = LocalizableStringDictionary.FormResourceUri(typeof(MyClass));
var stringDict = new LocalizableStringDictionary(resourceUri);

// For external files
var fileUri = new Uri(@"C:\MyApp\Localization\Messages.xaml", UriKind.Absolute);
var stringDict = new LocalizableStringDictionary(fileUri);

// Access localized strings
string message = stringDict.GetValueOrDefault("Welcome_Message", "Welcome!");

// The dictionary will automatically load appropriate translations based on current locale
```

**File Structure Example:**
```
MyApp/
├── Localization/
│   ├── en/
│   │   └── Messages.tsd
│   ├── fr/
│   │   └── Messages.tsd
│   └── Messages.xaml (native file)
```

**Native File Format (Messages.xaml):**
```xml
<LocalizableStringDictionary>
    <String Key="Welcome_Message" Value="Welcome!"/>
    <String Key="Goodbye_Message" Value="Goodbye!"/>
    <String Key="Error_Message" Value="An error occurred"/>
</LocalizableStringDictionary>
```

**Translation File Format (en/Messages.tsd, fr/Messages.tsd):**
```xml
<LocalizableStringDictionary>
    <String Key="Welcome_Message" Value="Bienvenue!"/>
    <String Key="Goodbye_Message" Value="Au revoir!"/>
    <String Key="Error_Message" Value="Une erreur s'est produite"/>
</LocalizableStringDictionary>
```

### Best Practices

1. **Embedded Resources**: Set build action to "Embedded resource" for native files in your project
2. **Translation Files**: Set "Copy to output directory" to "Copy if newer" for translation files  
3. **Key Naming**: Use descriptive, hierarchical key names (e.g., `Dialog_Save_Button_Text`)
4. **Default Values**: Always provide meaningful default values with `GetValueOrDefault`
5. **Parameterized Strings**: Include parameter placeholders in keys for clarity
6. **Error Handling**: Handle missing translations gracefully using the `TranslationLoadBehavior` setting

### Advanced Usage

```csharp
// Create custom localization target
public class MyComponent : ILocalizationTarget
{
    public LocaleInfo CurrentLocale { get; private set; } = LocaleInfo.Invalid;
    
    public void OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)
    {
        CurrentLocale = args.NewLocale;
        // Update component's localized content
        RefreshLocalizedContent();
    }
    
    private void RefreshLocalizedContent()
    {
        // Update UI elements, messages, etc.
    }
}

// Register with localization manager
var component = new MyComponent();
LocalizationManager.Default.Targets.Add(component);

// Programmatically manage translation files
var stringDict = new LocalizableStringDictionary();
stringDict.CreateTranslation(new LocaleInfo("es")); // Create Spanish translation
stringDict.LoadTranslation("es");
stringDict.UpdateTranslations(new[] {
    new KeyValuePair<string, string>("Welcome_Message", "¡Bienvenido!")
});
stringDict.SaveTranslation();
```

### Use Localization.Designer

- Localization Designer source code is available [here](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Designer).
- Instead of manually creating translation files for each language, Localization Designer can be used to easily load & translate localizable files. It will create corresponding translation files in appropriate directories.
- Set "Copy to output directory" of generated translation files to "Copy if newer" in Visual Studio. Those will appear in the appropriate Localization subfolders in the bin directory.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License - see the [LICENSE](../../LICENSE) file for details.
