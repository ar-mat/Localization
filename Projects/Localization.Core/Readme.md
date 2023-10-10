# Armat Localization Core

The document describes `Armat.Localization.Core` .Net library usage - providing a mechanisms to localize .Net applications. Following is main functionality supported by `Armat.Localization.Core`:
- Enumerating supported languages by the application
- Switching between languages at runtime
- Simple text localization for any .Net application
- Supporting localization for different resource formats and application types

Note: This is the core module of `Armat.Localization` library. It can be used for localizing strings for all types of .Net applications.
Below are derived libraries for specialized application types:

- Armat.Localization.Wpf can be used for localizing Wpf Resource Dictionaries. See [here](https://github.com/ar-mat/toolset/tree/main/Localization/Localization.Wpf) for more information.


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

- `CreateDefaultInstance` static method can be used at application startup to instantiate the default singleton instance of `LocalizationManager` class. It has an overload to accept `Configuration` parameter, and can be called only once before any usage of `LocalizationManager.Default`. If not created explicitly, the one with default configuration wil be created automatically.
- `Default` static property representing the singleton instance of `LocalizationManager` class to be used across the application.
- `Configuration` property describes the Localization Manager configuration of `Armat.Localization.Configuration` type.
- `AllLocales` property enumerates collection of `LocaleInfo` objects. These are the locales that application has translations for.
- `CurrentLocale` read-only property returns the currently selected `LocaleInfo` for the application.
- `ChangeLocale` method allows switching between supported locales at application runtime. There are overloads accepting either `LocaleInfo` or the locale name `String` parameters.

Note: Considering there could be more then one instance of `LocalizationManager` objects, invocation of `ChangeLocale` method doesn't change values of `Thread.CurrentThread.CurrentCulture` or `Thread.CurrentThread.CurrentUICulture` static properties, considering application will change it's thread(s) culture(s) based on the appropriate language change callback event.

Note: Passing *LocaleInfo.Invalid* locale to `LocalizationManager.ChangeLocale` will reset application language to the native locale.

There are more `LocalizationManager` APIs to be used by localizable resource containers. Description for those will be provided together with the usage.

### `Configuration` record struct

Describes configuration parameters for `LocalizationManager` class provided at construction time.

- `DefaultLocale` is a nullable `LocaleInfo` field referring to the locale used at application startup. In case of *null* value the *[Native]* locale will be used.
- `TranslationsDirectoryPath` points to the absolute or relative path to localizable resources translation directory.
- `LoadBehavior` is an enumeration of `TranslationLoadBehavior` type with possible values of `KeepNative` (default value), `ClearNative` and `RemoveNative`.

Note: `LocalizationManager.AllLocales` will return a special *[Native]* locale additional to the other locales if *Configuration.DefaultLocale == LocaleInfo.Invalid*. This is the default behavior.

### `LocalizableStringDictionary` class

Represents a type derived from `Dictionary<String, String>` of key-value pairs. The dictionary key (aka resource key) is used to retrieve appropriate localized values.

`LocalizableStringDictionary` can be loaded from an assembly embedded resource, as well as from a local file. The class is used for retrieving translated strings at application runtime.
Following is the format for `LocalizableStringDictionary` resource files:

```
<LocalizableStringDictionary>

	<String Key="MessageBox_Caption_Info" Value="Information"/>
	<String Key="MessageBox_Caption_Warning" Value="Warning"/>
	<String Key="MessageBox_Caption_Error" Value="Error"/>

</LocalizableStringDictionary>
```

`LocalizableStringDictionary` class has the following members:
- `LocalizationManager` property can be set to attach the string dictionary instance to the localization manager. Upon locale change in the localization manager, the appropriate translation will be loaded in the `LocalizableStringDictionary`.
- `Source` property is used to load the native resource file at a given `Url`. `Url` formatting is done in a following way:
	- For embedded resource files the `Source` Url must be constructed with uriString: `{AssemblyName};componnet/{ManifestResourceStreamPath}` and uriKind: `UriKind.Relative`.
	- For local file paths `Source` Url must be constructed with uriString: `path to the localizable file` and uriKind: `UriKind.Absolute`.
- `TranslationsDirRelativePath` property holds the path to the translations directory in case *LocalizationManager.Configuration.TranslationsDirectoryPath* should be overridden.
- `CurrentLocale` property represents the `LocaleInfo` currently loaded in the string dictionary.
- `GetValueOrDefault` method can be used to retrieve the translated value from dictionary if available, or the given default value otherwise. This is the API to be used at runtime to read localized values for the given key.

There are more `LocalizableStringDictionary` APIs used internally. Description for those will be provided together with the usage.


## General Usage Pattern

The usage pattern will be demonstrated on a sample of Wpf application.
It's available through [Armat Localization Demo](https://github.com/ar-mat/toolset/tree/main/Localization/Demo) GitHub link.

### Create LocalizationManager

- Create LocalizationManager `Configuration` by specifying the default locale for the application.
- Instantiate the default `LocalizationManager` upon application startup via `var lm = LocalizationManager.CreateDefaultInstance(cfg);`.
- Select application runtime language via `lm.ChangeLocale("en");` in case it is different from the default locale.
- Register to `LocalizationManager.Default.LocalizationChanged` event wherever the application must react to the language selection operation.

### Create LocalizableStringDictionary

- Create an xml string dictionary file in *Localization* folder of the project. Format of the localizable file is described above.
- Change the "Build action" of localizable string dictionary files to "Embedded resource", so it will build with the assembly.
- Define keys and values in localizable string dictionary file. Values should be defined in native language and will be used in case the translations aren't found / loaded.
- Create a class within the same directory and with the same name as the localizable string dictionary file. It can contain .Net code to access localizable strings.
- Define static properties in .Net class for accessing Localizable String Dictionary contents.
- Prefer using LocalizableStringDictionary.GetValueOrDefault to read localized strings. This will ensure that default hardcoded values will be returned in case of the missing keys in the dictionary.
- In case of parametrized strings, prefer putting parameter names in keys with the right order (see the 'InfoMessage_Parametrized_Lang_Text' key in demo sample application).

### Use Localization.Designer

- Localization Designer source code is available [here](https://github.com/ar-mat/toolset/tree/main/Localization/Localization.Designer).
- Instead of manually creating translation files for each language, Localization Designer can be used to easily load & translate localizable files. It will create corresponding translation files in appropriate directories.
- Set "Copy to output directory" of generated translation files to "Copy if newer" in Visual Studio. Those will appear in the appropriate Localization subfolders in the bin directory.
