# Armat Localization Demo Projects

This directory contains complete demo applications that showcase the capabilities of the Armat Localization library. These examples demonstrate best practices and provide working code that you can use as a reference for your own applications.

## 📚 Demo Applications

### [ClassLibrary](ClassLibrary/) - Core Localization Demo

A .NET class library demonstrating the core localization functionality using `Armat.Localization.Core`.

**What it demonstrates:**
- Basic `LocalizableStringDictionary` usage
- Embedded resource loading
- Proper static property patterns
- Core localization manager integration

**Key Files:**
- `StringDictionary.xaml` - Native localizable resource file
- `Localization/` - Translation files for multiple languages
- Core API usage patterns

### [WpfApp](WpfApp/) - Complete WPF Application Demo

A full-featured WPF application showing comprehensive localization implementation using both `Armat.Localization.Core` and `Armat.Localization.Wpf`.

**What it demonstrates:**
- WPF Resource Dictionary localization
- Runtime language switching with UI updates
- Menu and status bar localization
- Mixed XAML and code-behind localization
- Multiple resource dictionary management
- Language selection UI

**Key Files:**
- `MainWindow.xaml` - Localized WPF window with DynamicResource bindings
- `Localization/` - WPF resource dictionaries and translations
- Complete application lifecycle with localization

## 🚀 Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 (recommended) or Visual Studio Code
- Basic familiarity with .NET and WPF (for WPF demo)

### Running the Demos

1. **Open the solution:**
   ```bash
   cd Solution/Armat.Localization
   start Armat.Localization.sln
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Run the WPF Demo:**
   ```bash
   dotnet run --project ../../Projects/Demo/WpfApp
   ```

### Exploring the Code

Both demo projects are extensively commented and follow the recommended patterns described in the main documentation. They serve as:

- **Learning resources** - Understand how to implement localization
- **Testing ground** - Experiment with different localization scenarios
- **Code templates** - Copy patterns to your own applications
- **Validation tools** - Verify library functionality

## 🌐 Supported Languages

The demo applications include translations for:

- **English (en)** - Default/native language
- **Armenian (hy)** - Armenian language support
- **Russian (ru)** - Russian language support

You can switch between languages at runtime using the language menu in the WPF application.

## 📁 Directory Structure

```
Demo/
├── README.md                 # This file
├── ClassLibrary/             # Core localization demo
│   ├── ClassLibrary.csproj   # Project file
│   ├── StringDictionary.cs   # Localization wrapper class
│   └── Localization/         # Localizable resources
│       ├── StringDictionary.xaml  # Native resources
│       ├── en/               # English translations
│       ├── hy/               # Armenian translations
│       └── ru/               # Russian translations
└── WpfApp/                   # WPF application demo
    ├── WpfApp.csproj         # Project file
    ├── App.xaml              # Application definition
    ├── MainWindow.xaml       # Main window with localized UI
    ├── MainWindow.xaml.cs    # Code-behind with localization
    └── Localization/         # WPF localizable resources
        ├── *.xaml            # Native WPF resource dictionaries
        ├── en/               # English translations (.trd files)
        ├── hy/               # Armenian translations
        └── ru/               # Russian translations
```

## 🎯 Key Learning Points

### From ClassLibrary Demo:

1. **Resource File Structure** - How to organize localizable string resources
2. **Static Property Pattern** - Best practice for exposing localized strings
3. **Embedded Resources** - Using assembly embedded resources for distribution
4. **Fallback Values** - Providing default values for missing translations
5. **Manager Integration** - Connecting string dictionaries to localization manager

### From WpfApp Demo:

1. **XAML Localization** - Using `DynamicResource` for runtime language switching
2. **Resource Dictionary Merging** - Organizing multiple resource dictionaries
3. **Language Menu Implementation** - Creating user-friendly language selection
4. **Mixed Localization** - Combining XAML and code-behind localization
5. **Event Handling** - Responding to language change events
6. **Status Bar Updates** - Dynamic UI updates on language change

## 🔧 Customization

### Adding New Languages

1. **Create translation directories:**
   ```bash
   mkdir bin/Debug/Localization/es
   mkdir bin/Debug/Localization/de
   ```

2. **Create translation files:**
   - For ClassLibrary: `*.tsd` files
   - For WpfApp: `*.trd` files

3. **Use Localization.Designer tool** to create and manage translations efficiently

### Modifying Examples

Feel free to:
- Add new localizable strings
- Create additional resource dictionaries
- Implement new UI elements with localization
- Experiment with different language switching mechanisms
- Test edge cases and error handling

## 🛠️ Troubleshooting

### Common Issues:

1. **Missing translations** - Check file paths and ensure translation files are copied to output
2. **Resources not updating** - Verify `DynamicResource` is used instead of `StaticResource`
3. **Build errors** - Ensure all packages are restored: `dotnet restore`
4. **Runtime errors** - Check embedded resource names match exactly

### Debug Tips:

- Enable verbose logging in LocalizationManager configuration
- Use debugger to inspect resource loading
- Verify file existence in output directory
- Test with minimal example first

## 📞 Support

If you encounter issues with the demo applications:

- 🐛 **Report Issues**: [GitHub Issues](https://github.com/ar-mat/Localization/issues)
- 📖 **Documentation**: [Main README](../../README.md)
- 💬 **Discussions**: [GitHub Discussions](https://github.com/ar-mat/Localization/discussions)

## 🤝 Contributing

Improvements to the demo applications are welcome! Consider contributing:

- Additional language translations
- More complex localization scenarios
- Better UI/UX examples
- Performance optimization examples
- Error handling demonstrations

See the main repository [contributing guidelines](../../CONTRIBUTING.md) for details.