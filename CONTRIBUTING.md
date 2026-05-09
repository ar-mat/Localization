# Contributing to Armat Localization

Thank you for contributing! This document covers the essential guidelines for contributing to the Armat Localization library.

## 🚀 Quick Start

### Prerequisites
- .NET 10 SDK
- Git
- Visual Studio 2022 17.10+ or VS Code
- MAUI workload installed (only required when touching the MAUI library or demo): `dotnet workload install maui`

### Setup
```bash
git clone https://github.com/ar-mat/Localization.git
cd Localization
dotnet restore Solution/Armat.Localization/Armat.Localization.sln
dotnet build Solution/Armat.Localization/Armat.Localization.sln
```

## 📋 Code Style

### Naming Conventions
- **Types / methods / properties / events / public constants**: `PascalCase`.
- **Local variables / parameters**: `camelCase`.
- **Private instance fields**: `_camelCase` (leading underscore).
- **Private static `const` / `readonly` backing fields used by static accessors**: `_camelCase` is also used (see e.g. `_nativeFileExt`, `_nativeFileExtArray`). Reserve `PascalCase` for properties / public surface.

### Formatting
- **Indentation**: tabs. No spaces for indentation.
- **Braces**: Allman style — every `{` and `}` on its own line, including for one-line `if` bodies that span multiple statements. One-line `if`-without-braces is acceptable when the body is a single statement.
- **Types**: prefer BCL aliases throughout — `String`, `Int32`, `Boolean`, `Object`, `Char`, `Double`, `Byte`, etc. — not `string`, `int`, `bool`. The codebase is fully consistent on this; see `Localization.Import.csproj` (`<EnforceCodeStyleInBuild>True</EnforceCodeStyleInBuild>`).
- **`using` directives**: at the top of the file outside the namespace, grouped (`Microsoft.*` separately from `System.*`), with a blank line between groups. File-scoped namespaces are used everywhere.
- **Nullable reference types**: enabled solution-wide. Annotate nullable parameters / fields explicitly with `?`. Don't suppress with `!` unless the non-null invariant is enforced elsewhere and the suppression is intentional — match the existing comments next to such suppressions when adding more (e.g. `// value can't be null because of above condition checks`).

### Example
```csharp
public class LocalizationManager
{
	// the singleton instance
	private static LocalizationManager? _default = null;

	// Read-only instance properties
	public Configuration Configuration { get; private set; }

	public String GetTranslation(String key)
	{
		if (String.IsNullOrEmpty(key))
			return String.Empty;

		return _translations[key];
	}
}
```

## 💬 Comment Style

The codebase favors **plain `//` line comments** over XML doc comments. Follow these patterns when adding or updating comments:

### When to comment

1. **Section / region headers** — single-line `//` comments above logically grouped fields or methods. They aren't full sentences; they label what follows.
   ```csharp
   // the logger to be used for this class
   protected ILogger Logger { get; private set; }

   // Localization manager
   private LocalizationManager? _locMgr = null;

   // Implementation of ILocalizationTarget interface
   private LocaleInfo _currLocale;
   ```

2. **Step-by-step intent inside a method** — short `//` comments above small logical blocks describe *what the next few lines do*, in the imperative or as a noun phrase. Lowercase first letter is common (`// check if already created`, `// remember the old locale`, `// register in the localization manager`).
   ```csharp
   public void ChangeLocale(LocaleInfo locale)
   {
   	// remember the old locale
   	LocaleInfo oldLocale = CurrentLocale;

   	// update the current locale
   	CurrentLocale = locale;

   	// update all Localization Targets
   	for (Int32 index = 0; index < _listTargets.Count; index++)
   	{
   		...
   	}
   }
   ```

3. **WHY for non-obvious decisions** — multi-line `//` blocks explaining a constraint, a quirk of an external API, or a deliberate design choice. These are the highest-value comments in the codebase.
   ```csharp
   // MAUI does not call ISupportInitialize.EndInit() for ResourceDictionary subclasses.
   // Subscribe to ValuesChanged: it fires when MAUI finishes loading the native XAML content,
   // which is the reliable post-initialization hook for ResourceDictionary subclasses.
   ((IResourceDictionary)this).ValuesChanged += OnNativeValuesLoaded;
   ```

   ```csharp
   // Be careful registering delegated to this event. It may hold a reference to your object,
   // preventing its finalization. Do not provide an event delegate from a class instance which
   // can be disposed during the program lifetime. Instead, an instance of  ILocalizationTarget
   // can be registered to receive locale change events.
   public event LocalizationChangeEventHandler? LocalizationChanged;
   ```

4. **Pragma / suppression rationale** — when adding `#pragma warning disable`, name the warning code and let the surrounding code speak for the rationale; keep the disable scoped to the smallest region.

### When NOT to comment

- **Don't restate code.** `// increment counter` above `counter++;` is noise.
- **Don't duplicate the method name.** `// gets the current locale` above `public LocaleInfo GetCurrentLocale()` adds nothing.
- **Don't leave commented-out code in committed PRs** unless the surrounding comments explain why it's there for reference (the `//if (!locale.IsValid)` block in `LocalizationManager.ChangeLocale` is a deliberate "we removed this guard, here's the alternative we considered" reference — most other dead code should be deleted).

### XML doc comments (`///`)

XML doc comments are used **sparingly** and almost exclusively for boilerplate `Interaction logic for X.xaml` summaries on auto-generated WPF code-behind partial classes. Don't add `/// <summary>` comments to internal helpers or private methods — the existing code uses `//` for those.

When you do add public-API XML docs (e.g. on a new package's surface), keep them concise and match the existing tone in the per-project `Readme.md` files.

### Region directives (`#region` / `#endregion`)

Used in larger files (`MainWindow.xaml.cs`, `LocalizationManager.cs`) to group related members. The closing `#endregion` is annotated with the region name, e.g. `#endregion // UI command handlers`. Match this convention.

## 🔄 Workflow

**All contributions must be submitted via pull requests.** Direct commits to `main` are not allowed.

1. **Fork** the repository to your GitHub account.
2. **Create a feature branch** from `main`.
3. **Make changes** following the code style and comment guidelines above.
4. **Build** the solution and verify there are no new warnings.
5. **Test** your changes thoroughly, including the relevant Demo app where applicable.
6. **Commit** with clear, descriptive messages: `feat(core): add new feature`, `fix(maui): handle missing asset`, `docs(designer): update README`.
7. **Push** your branch to your fork.
8. **Submit a pull request** to `main`.
9. **Wait for review** and address any feedback from maintainers.

### Pull Request Guidelines
- Provide a clear title and description.
- Reference any related issues using `#issue-number`.
- Ensure the solution builds clean (`dotnet build Solution/Armat.Localization/Armat.Localization.sln`).
- Keep pull requests focused on a single feature or fix.
- Update the relevant `Readme.md` / `README.md` and `CLAUDE.md` if your change alters public API, file layout, or build behavior.

## 🐛 Issues & Features

### Bug Reports
Include:
- Steps to reproduce.
- Expected vs actual behavior.
- Environment details (.NET version, OS, target framework, MAUI workload version when relevant).

### Feature Requests
Include:
- Problem description.
- Proposed solution.
- Use cases.

## 📝 Documentation

- Keep public-facing docs in the per-project `Readme.md` files in sync with the code.
- The root `CLAUDE.md` captures repository-wide conventions and gotchas — update it whenever those change.
- Include code examples that compile against the current API, not historical signatures.

---

Questions? Open an issue or start a discussion.
