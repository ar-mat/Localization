# Armat Localization Designer

![Application Icon](Resources/AppIcon.png)

The **Armat Localization Designer** is a powerful WPF desktop application for managing translations in Armat Localization projects. It provides an intuitive graphical interface for creating, editing, and maintaining translation files, making the localization workflow efficient and user-friendly.

## ✨ Features

- **Visual Translation Management** - Intuitive table-based interface for managing translations
- **Multiple File Support** - Load and manage multiple localizable resource files simultaneously
- **Language Management** - Add and remove supported languages dynamically
- **Real-time Editing** - Edit translations with immediate visual feedback
- **File Format Detection** - Automatic detection of String Dictionary (.xaml) and WPF Resource Dictionary formats
- **Bulk Operations** - Save all translations across all languages at once
- **Directory Scanning** - Automatically discover localizable files in project directories
- **Translation Validation** - Visual indicators for missing or incomplete translations
- **Export Functionality** - Generate translation files in the correct format and directory structure

## 🚀 Installation & Usage

### Download

Download the latest release from the [GitHub Releases](https://github.com/ar-mat/Localization/releases) page.

### System Requirements

- Windows 10 or later
- .NET 8.0 Runtime (automatically installed if missing)
- Sufficient disk space for translation files

### Quick Start

1. **Launch the Application**
   - Run `armat.localization.designer.exe`
   - The application will start with an empty project

2. **Load Localizable Files**
   - **Option A**: Use "Scan Directory" to automatically discover files
   - **Option B**: Use "Add Files" to manually select specific files
   - **Option C**: Drag and drop files directly into the application

3. **Manage Languages**
   - Click "Add Language" to add new target languages
   - Select from comprehensive list of supported locales
   - Remove languages using "Remove Language" button

4. **Edit Translations**
   - Select a language tab to view/edit translations for that language
   - Edit text directly in the translation table
   - Missing translations are highlighted for easy identification

5. **Save Translations**
   - Use "Save All" to save translations for all languages
   - Files are automatically saved in the correct directory structure
   - Translation files are created in appropriate format (`.tsd` or `.trd`)

## 🏗️ User Interface

### Main Window Components

#### Toolbar
- **📂 Scan Directory** - Recursively scan directory for localizable files
- **➕ Add Files** - Manually add specific localizable files
- **❌ Remove Selected** - Remove selected files from the project
- **🗑️ Clear Files** - Remove all files from the current project
- **💾 Save All** - Save all translations across all languages

#### File List Panel
- **File Type Icons** - Visual indicators for different file types:
  - 📄 String Dictionary (`.xaml` with `LocalizableStringDictionary`)
  - 🎨 WPF Resource Dictionary (`.xaml` with `LocalizableResourceDictionary`)
  - ❓ Unknown format
- **File Paths** - Full paths to loaded localizable files
- **Status Indicators** - Visual feedback on file loading status

#### Translation Management
- **Language Tabs** - Switch between different target languages
- **Translation Table** - Grid view showing:
  - **Key** - Resource key from the original file
  - **Original Value** - Native language value
  - **Translation** - Target language translation (editable)
- **Progress Indicators** - Visual progress of translation completion

#### Language Management Panel
- **➕ Add Language** - Add new target languages
- **❌ Remove Language** - Remove selected languages
- **Language List** - Currently configured target languages

## 📁 Supported File Formats

### Input Formats (Native Files)

#### String Dictionary Format
```xml
<LocalizableStringDictionary>
    <String Key="WelcomeMessage" Value="Welcome to our application!"/>
    <String Key="ErrorMessage" Value="An error occurred."/>
    <String Key="ConfirmExit" Value="Are you sure you want to exit?"/>
</LocalizableStringDictionary>
```

#### WPF Resource Dictionary Format
```xml
<l_wpf:LocalizableResourceDictionary 
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:s="clr-namespace:System;assembly=netstandard"
    xmlns:l_wpf="clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf">

    <s:String x:Key="WindowTitle">My Application</s:String>
    <s:String x:Key="FileMenu">File</s:String>
    <s:String x:Key="SaveCommand">Save</s:String>

</l_wpf:LocalizableResourceDictionary>
```

### Output Formats (Translation Files)

#### String Dictionary Translation (`.tsd`)
```xml
<LocalizableStringDictionary>
    <String Key="WelcomeMessage" Value="Bienvenue dans notre application!"/>
    <String Key="ErrorMessage" Value="Une erreur s'est produite."/>
    <String Key="ConfirmExit" Value="Êtes-vous sûr de vouloir quitter?"/>
</LocalizableStringDictionary>
```

#### WPF Resource Dictionary Translation (`.trd`)
```xml
<l_wpf:LocalizableResourceDictionary 
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:s="clr-namespace:System;assembly=netstandard"
    xmlns:l_wpf="clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf">

    <s:String x:Key="WindowTitle">Mon Application</s:String>
    <s:String x:Key="FileMenu">Fichier</s:String>
    <s:String x:Key="SaveCommand">Enregistrer</s:String>

</l_wpf:LocalizableResourceDictionary>
```

## 🔄 Workflow Integration

### Visual Studio Integration

1. **Add localizable files** to your project with "Embedded Resource" build action
2. **Use Localization Designer** to create translations
3. **Set translation files** to "Copy if newer" in Visual Studio
4. **Build and run** your application with multi-language support

### Recommended Project Structure

```
YourProject/
├── Localization/              # Native localizable files (embedded)
│   ├── Messages.xaml
│   ├── UIStrings.xaml
│   └── ErrorMessages.xaml
├── bin/Debug/Localization/    # Generated translation files
│   ├── en/
│   │   ├── Messages.tsd
│   │   ├── UIStrings.trd
│   │   └── ErrorMessages.tsd
│   ├── fr/
│   │   ├── Messages.tsd
│   │   ├── UIStrings.trd
│   │   └── ErrorMessages.tsd
│   └── es/
│       ├── Messages.tsd
│       ├── UIStrings.trd
│       └── ErrorMessages.tsd
└── YourProject.exe
```

## 🎯 Best Practices

### File Organization

1. **Group related strings** - Organize strings into logical files (UI, Messages, Errors)
2. **Consistent naming** - Use consistent key naming conventions
3. **Descriptive keys** - Choose keys that clearly describe the content
4. **Avoid special characters** - Use alphanumeric characters and underscores in keys

### Translation Management

1. **Regular backups** - Keep backups of translation files
2. **Version control** - Include translation files in version control
3. **Translator instructions** - Provide context and guidelines for translators
4. **Quality review** - Review translations for consistency and accuracy
5. **Testing** - Test applications with different languages and text lengths

### Workflow Optimization

1. **Scan directories** for efficient file discovery
2. **Batch operations** to save time on large projects
3. **Language prioritization** - Start with most important languages
4. **Incremental updates** - Update translations as source content changes

## 🔧 Advanced Features

### Keyboard Shortcuts

- **Ctrl+O** - Open/Add files
- **Ctrl+S** - Save all translations
- **Delete** - Remove selected files
- **F5** - Refresh file list
- **Tab/Shift+Tab** - Navigate translation table

### Command Line Support

While primarily a GUI application, the Designer can be automated through file system operations:

```powershell
# Scan and prepare translation files
& "armat.localization.designer.exe" --scan "C:\MyProject\Localization"
```

### Language Code Support

The application supports standard ISO language codes:
- **Two-letter codes**: `en`, `fr`, `de`, `es`, `it`, etc.
- **Culture-specific codes**: `en-US`, `en-GB`, `fr-FR`, `fr-CA`, etc.
- **Regional variants**: Full support for regional language variations

## 🛠️ Troubleshooting

### Common Issues

**Files not loading:**
- Verify file format matches expected XML structure
- Check file permissions and paths
- Ensure valid XML syntax

**Translations not saving:**
- Check write permissions in target directory
- Verify available disk space
- Ensure target language directories exist

**Missing translations in application:**
- Verify translation files are in correct output directory
- Check file copy settings in Visual Studio
- Ensure correct file naming convention

### Debug Information

The application logs important operations and errors. Check the application output for detailed information about:
- File loading operations
- Translation saving processes
- Language management actions
- Error conditions and resolutions

## 📞 Support & Feedback

- 🐛 **Bug Reports**: [GitHub Issues](https://github.com/ar-mat/Localization/issues)
- 💡 **Feature Requests**: [GitHub Discussions](https://github.com/ar-mat/Localization/discussions)
- 📖 **Documentation**: [Project Documentation](https://github.com/ar-mat/Localization)
- 🌐 **Website**: [armat.am/products/localization](http://armat.am/products/localization)

## 🔗 Related Components

- **[Armat.Localization.Core](../Localization.Core)** - Core localization library
- **[Armat.Localization.Wpf](../Localization.Wpf)** - WPF localization extensions  
- **[Demo Applications](../Demo)** - Example implementations
- **[Main Documentation](../../README.md)** - Complete solution overview

## 📄 License

This application is part of the Armat Localization library and is licensed under the MIT License. See the main [LICENSE](../../LICENSE) file for details.