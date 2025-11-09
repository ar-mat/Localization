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

Represents a wrapper over `System.Globalization.CultureInfo` class, specializing it for `Armat.Localization.Core` library. Implements `IComparable<LocaleInfo>` for sorting capabilities.

- `Invalid` static property returns a singleton instance of `LocaleInfo` class to be used for *null* or *[Native]* locale with display name "[Native]".
- `AllLocales` static property for enumerating all locales. It's based on `System.Globalization.CultureTypes.AllCultures`, and provides a comprehensive list of locales ordered by `DisplayName`.
- `Culture` nullable property provides the underlying instance of `System.Globalization.CultureInfo` class.
- `DisplayNameOverride` property allows overriding the display name at construction time.
- `IsValid` property returns `true` when `Culture != null && Culture != CultureInfo.InvariantCulture`.
- `Name` property returns the wrapped `CultureInfo.Name` for valid locales, and empty string for invalid locales.
- `DisplayName` property returns `DisplayNameOverride` if set, otherwise returns `Culture.DisplayName` for valid locales.
- `CompareTo` method provides comparison by locale names for consistent sorting.
- `ToString()` method returns the `DisplayName` value.

The class supports multiple constructors: parameterless (for Invalid), from `CultureInfo`, from `CultureInfo` with display name override, and from locale name string.

### `LocalizationManager` class

Acts as a central component of `Armat.Localization.Core` library, providing singleton and instance-based localization management.

**Static Methods:**
- `CreateDefaultInstance()` static method creates the default singleton instance with default configuration and null logger factory.
- `CreateDefaultInstance(Configuration config)` creates the default singleton with specified configuration.
- `CreateDefaultInstance(ILoggerFactory loggerFactory)` creates the default singleton with specified logger factory.
- `CreateDefaultInstance(Configuration config, ILoggerFactory loggerFactory)` creates the default singleton with both parameters. Can only be called once before any usage of `LocalizationManager.Default`.
- `CreateInstance(Configuration config)` creates a new non-singleton instance using the default logger factory.
- `CreateInstance(Configuration config, ILoggerFactory loggerFactory)` creates a new non-singleton instance with specified parameters.

**Properties:**
- `Default` static property representing the singleton instance. Returns a non-operational instance if not explicitly created.
- `Configuration` init-only property describes the localization manager configuration.
- `LoggerFactory` init-only property provides the logger factory used for creating loggers.
- `CurrentLocale` read-only property returns the currently selected `LocaleInfo` for the application.
- `AllLocales` property enumerates collection of `LocaleInfo` objects by scanning the translations directory structure. Returns locales ordered by `DisplayName` with the default locale appearing first if not already included.
- `Targets` collection property manages `ILocalizationTarget` objects that receive locale change notifications. Uses weak references for automatic cleanup.

**Methods:**
- `ChangeLocale(String localeName)` and `ChangeLocale(LocaleInfo locale)` methods allow switching between supported locales at application runtime.
- `GetTranslationsDirectory()` returns `DirectoryInfo` for the base translations directory, resolving relative paths against the entry assembly location.
- `GetTranslationsDirectory(String localeName)` returns `DirectoryInfo` for a specific locale's translation directory.

**Events:**
- `LocalizationChanged` event is fired when the locale changes, allowing external components to respond to language switches.

The class uses weak references to manage localization targets, ensuring automatic cleanup when targets are disposed. It provides comprehensive logging through the Microsoft.Extensions.Logging framework.

### `Configuration` record struct

Describes configuration parameters for `LocalizationManager` class provided at construction time. Implements `IEquatable<Configuration>`.

- `DefaultLocale` is a nullable `LocaleInfo` property referring to the locale used at application startup. Default constructor sets this to `null`. If not null, the `LocalizationManager` class will initialize the `CurrentLocale` property using it's value.
- `TranslationsDirectoryPath` property points to the absolute or relative path to localizable resources translation directory. Default constructor sets this to empty string. It is required to have a non-empty value for the `LocalizationManager` to be able to locate translation the translation directories and files.
- `TranslationLoadBehavior` property is an enumeration of `TranslationLoadBehavior` type with possible values of `KeepNative` (default), `ClearNative` and `RemoveNative`. The property is used to determine the value of a localizable field if the translation file doesn't define the localized value.

The `Configuration.Default` static property provides default configuration values with `DefaultLocale = LocaleInfo.Invalid`, `TranslationsDirectoryPath = "Localization"`, and `TranslationLoadBehavior = TranslationLoadBehavior.KeepNative`.

Note: `LocalizationManager.AllLocales` will return a special *[Native]* locale additional to the other locales if *Configuration.DefaultLocale == LocaleInfo.Invalid*. This is the default behavior.

### `LocalizableStringDictionary` class

Represents a type derived from `Dictionary<String, String>` of key-value pairs that implements `ISupportInitialize`, `ILocalizationTarget`, and `ILocalizableResource` interfaces for comprehensive translation management.

**Constructors:**
- Default parameterless constructor
- Constructor with `String sourceUri` (uses default LocalizationManager)
- Constructor with `String sourceUri` and `LocalizationManager`
- Constructor with `Uri source` (uses default LocalizationManager) 
- Constructor with `Uri source` and `LocalizationManager`

**Properties:**
- `LocalizationManager` property attaches the string dictionary to a localization manager. Cannot be reset once set.
- `Source` property is used to load the native resource file at a given `Uri`. Setting it triggers auto-loading if the resource file path is available.
- `TranslationsDirRelativePath` property holds the path to translations directory override.
- `CurrentLocale` property represents the `LocaleInfo` currently loaded in the string dictionary.
- `NativeFileExtensions` and `TranslationFileExtensions` properties define supported file extensions ("xaml" for native files, "tsd" for translation files).
- `NativeFileExtension` and `TranslationFileExtension` static properties provide the individual extension strings.

**Methods:**
- `GetValueOrDefault(String key, String defaultValue)` method retrieves the translated value from dictionary if available, or the given default value otherwise.
- `FormResourceUri(Type type)` static method helps create proper URI from a Type for embedded resources using the format `/{AssemblyName};component/{TypeFullName}.xaml`.
- `LoadNative()` methods load native language content from file or embedded resource.
- `CanLoadNative(Uri sourceUri)` checks if the source can be loaded as a valid LocalizableStringDictionary.
- `LoadTranslation(String localeName)` and `LoadTranslation(LocaleInfo locale)` load translations for the given locale.
- `SaveTranslation()` saves the current locale translation to file.
- `CreateTranslation(LocaleInfo locale)` creates an empty translation file for the given locale.
- `DeleteTranslation(LocaleInfo locale)` deletes the translation file for the given locale.
- `Enumerate()` returns ordered key-value pairs for the current locale.
- `UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)` updates translations with new values.

**File Path Methods:**
- `ResourceFilePath` property returns the source URI path.
- `GetNativeFilePath()` returns absolute file path for file-based sources.
- `GetNativeResourceStream()` returns stream for embedded resource sources.
- `GetTranslationFilePath(LocaleInfo locale)` returns the full path to a locale's translation file.

The class automatically manages translation file loading based on localization manager events and supports both embedded resources and external files. URI formatting follows the pattern `{AssemblyName};component/{ManifestResourceStreamPath}` for embedded resources and absolute file paths for external files.


## Additional Interfaces and Types

### `ILocalizationTarget` Interface

Defines the contract for objects that need to receive locale change notifications:
- `CurrentLocale` property - Gets the current locale of the target
- `OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)` method - Called when the locale changes

### `ILocalizableResource` Interface  

Defines the contract for localizable resource management:
- `NativeFileExtensions` and `TranslationFileExtensions` properties - Supported file extensions arrays
- `Source` property - URI of the resource source
- `LoadNative(Uri sourceUri, LocalizationManager localizationManager)` method - Loads native language content with specified localization manager. It sets the `LocalizationManager` and `Source` properties in case those have not been initialized earlier.
- `LoadTranslation(LocaleInfo locale)`, `SaveTranslation()`, `CreateTranslation(LocaleInfo locale)`, `DeleteTranslation(LocaleInfo locale)` methods - Translation file management
- `Enumerate()` method - Returns key-value pairs for the current locale
- `UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)` method - Updates translations with new values

### `LocalizationChangeEventArgs` Class

Event arguments for locale change events, derived from `EventArgs`:
- `OldLocale` property - Previous locale (nullable, init-only)
- `NewLocale` property - New locale (init-only)
- Constructors: `LocalizationChangeEventArgs(LocaleInfo newLocale)` and `LocalizationChangeEventArgs(LocaleInfo? oldLocale, LocaleInfo newLocale)`

### `LocalizationChangeEventHandler` Delegate

Delegate type for localization change events: `delegate void LocalizationChangeEventHandler(object sender, LocalizationChangeEventArgs e)`

### `TranslationLoadBehavior` Enumeration

Defines how missing translations are handled:
- `KeepNative` - Keep original native values (default)
- `ClearNative` - Replace with empty strings
- `RemoveNative` - Remove keys entirely

### Serialization Support

#### `TextRecord` struct
- `Key` property - String key with XmlAttribute serialization
- `Value` property - String value with XmlAttribute serialization

#### `LocalizationDocument` class
- `RootXmlElementName` constant - "LocalizableStringDictionary"
- `Records` property - Array of `TextRecord` with XmlElement("String") serialization
- `Load(Stream stream)` static method - Deserializes from XML stream
- `Save(Stream stream)` method - Serializes to XML stream with indentation


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
