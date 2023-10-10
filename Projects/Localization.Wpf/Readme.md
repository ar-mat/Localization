# Armat Localization Wpf

The document describes `Armat Localization Wpf` .Net library. Being an extension of `Armat Localization Core`, it provides APIs for localizing Wpf applications.
See more details in https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Core.

## `LocalizableResourceDictionary` class

Represents a type derived from `ResourceDictionary` of Wpf resources. The dictionary key (aka resource key) is used to retrieve appropriate localized values.

`LocalizableResourceDictionary` can be loaded from Wpf assembly resource, as well as from a local file. The class is used for retrieving translated strings at application runtime.
Following is the format for `LocalizableResourceDictionary` resource files:

```
<l_wpf:LocalizableResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:System;assembly=netstandard"
                    xmlns:l_wpf="clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf">

    <s:String x:Key="MessageBox_Caption_Info">Information</s:String>
    <s:String x:Key="MessageBox_Caption_Warning">Warning</s:String>
    <s:String x:Key="MessageBox_Caption_Error">Error</s:String>

</l_wpf:LocalizableResourceDictionary>
```

`LocalizableResourceDictionary` class has the following members:
- `LocalizationManager` property can be set to attach the resource dictionary instance to the localization manager. Upon locale change in the localization manager, the appropriate translation will be loaded in the `LocalizableResourceDictionary`.
- `TranslationsDirRelativePath` property holds the path to the translations directory in case *LocalizationManager.Configuration.TranslationsDirectoryPath* should be overridden.
- `CurrentLocale` property represents the `LocaleInfo` currently loaded in the resource dictionary.
- `GetValueOrDefault` method can be used to retrieve the translated value from dictionary if available, or the given default value otherwise.

There are more `LocalizableResourceDictionary` APIs used internally. Description for those will be provided together with the usage.


## General Usage Pattern

The usage pattern will be demonstrated on a sample of Wpf application.
It's available through [Armat Localization Demo](https://github.com/ar-mat/Localization/tree/main/Projects/Demo) GitHub link.

### Create LocalizationManager

- Create LocalizationManager `Configuration` by specifying the default locale for the application.
- Instantiate the default `LocalizationManager` upon application startup via `var lm = LocalizationManager.CreateDefaultInstance(cfg);`.
- Select application runtime language via `lm.ChangeLocale("en");` in case it is different from the default locale.
- Register to `LocalizationManager.Default.LocalizationChanged` event wherever the application must react to the language selection operation.

### Create LocalizableResourceDictionary

- Create a Wpf Resource Dictionary file in *Localization* folder of the project. Format of the localizable file is described above.
- Define keys and values in localizable resource dictionary file. Values should be defined in native language and will be used in case the translations aren't found / loaded.
- Add the localizable resource dictionary as a merged dictionary in appropriate Wpf *xaml* controls and windows.
- Prefer using LocalizableResourceDictionary.GetValueOrDefault to read localized values in case the strings are used in a c# class, not in *xaml* files.

### Use Localization.Designer

- Localization Designer source code is available [here](https://github.com/ar-mat/Localization/tree/main/Projects/Localization.Designer).
- Instead of manually creating translation files for each language, Localization Designer can be used to easily load & translate localizable files. It will create corresponding translation files in appropriate directories.
- Set "Copy to output directory" of generated translation files to "Copy if newer" in Visual Studio. Those will appear in the appropriate Localization subfolders in the bin directory.
