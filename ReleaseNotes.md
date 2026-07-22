# Release Notes

<!--
How to maintain this file:
- One "## vX.Y.Z - YYYY-MM-DD" section per release, newest first.
- The top section describes the upcoming release (version taken from
  _ArmatLocalizationVersion + _NugetVersionPostfix in
  Projects/Localization.Import.csproj); replace "(unreleased)" with the release
  date when publishing, then start the next section on top for new changes.
- NuGet packages reference this file through <PackageReleaseNotes> in
  Projects/Localization.Import.csproj.
-->

## v3.0.0-beta (unreleased)

Changes since `v2.3.0-beta`. A full review of the code base was performed, and all confirmed findings were fixed. The version bump reflects the behavior changes listed under "Changed" below.

### Core (`armat.localization.core`)

Fixed:

- The `String` / `Uri` source constructors of `LocalizableStringDictionary` now load the native content; previously such dictionaries stayed permanently empty and only logged errors on locale changes.
- Translation paths for absolute file `Source`s now resolve to `<TranslationsDirectory>/<locale>/<file name>`; previously the locale directory was silently dropped, so every locale read and wrote the same translation file.
- Setting `Source = null` now unloads the dictionary contents; previously it cleared the contents and then threw `InvalidOperationException`.
- Relative `TranslationsDirectoryPath` values resolve against `AppContext.BaseDirectory`, fixing single-file published applications.
- `LocalizationManager.CreateDefaultInstance` is thread-safe, and the placeholder manager returned by `LocalizationManager.Default` before initialization carries valid default configuration (its translations path used to be null). A warning is now logged when localizable resources were registered before the default manager was created — such targets do not follow the default manager's locale changes.
- The locale directory scan behind `LocalizationManager.AllLocales` no longer treats arbitrary well-formed directory names as cultures on ICU platforms (predefined-only culture lookup).
- Translation and native XML files are loaded with DTD processing prohibited.
- Failures to load the resource assembly for embedded sources preserve the original exception as `InnerException`.

Changed:

- Empty-string values in translation files are treated as "not translated" — `TranslationLoadBehavior` decides what happens to them, so partially translated files fall back to native text under the default `KeepNative` instead of showing blank strings.
- A failed `LoadTranslation` (invalid locale or missing translation file) returns `false` and leaves the dictionary contents and `CurrentLocale` fully unchanged; previously the new locale was reported as current and the load behavior could clear or remove the contents.
- `LocaleInfo` equality is based on the culture `Name` only; `DisplayNameOverride` is cosmetic and no longer participates in comparisons. `CompareTo` uses ordinal comparison.

### WPF (`armat.localization.wpf`)

- All Core behavior changes above (empty-value handling, failure semantics, translation path resolution for absolute sources) apply to the WPF `LocalizableResourceDictionary` as well.

### MAUI (`armat.localization.maui`)

Fixed:

- Switching back to the native locale now restores the original values by reading MAUI's internal merged instance (the pristine native dictionary MAUI keeps when loading from `Source`); previously it was a silent no-op that left the translated values in place (MAUI does not allow re-assigning `ResourceDictionary.Source` from code).
- `LoadNative(Uri, LocalizationManager)` throws a descriptive `NotSupportedException` instead of failing with `InvalidCastException`. The `Uri`-taking constructors are kept but currently fail at runtime with MAUI's own "Source can only be set from XAML" exception.
- Failures while probing app-package translation assets are logged; previously they were swallowed silently.
- Empty keys and empty values in translation files are skipped, matching Core semantics.
- Removed the LocalizableResourceDictionary constructors with arguments `Uri` source that MAUI does not support.

### Localization Designer

Fixed:

- Closing the window saves pending translations synchronously; previously the fire-and-forget save could race application exit and lose edits. Save errors now show an error dialog instead of crashing the application.
- Load and save operations are properly serialized; the previous mutex was effectively released before the guarded work ran, allowing concurrent file access.
- Saving writes only the cells that actually changed and no longer creates translation files for locales a resource file never had; previously editing one cell could rewrite every locale column and create phantom translation files filled with empty values.
- Missing translations are cached and shown as empty columns, avoiding a full re-parse of the native file on every UI refresh.
- MAUI file support keeps its converted temp file alive for the lifetime of the loaded resource (locale operations can no longer hit a deleted temp file) and cleans it up when the file entry is reset.
- Error dialogs are marshalled to the UI thread; the localized UI strings singleton is thread-safe.

### Documentation and repository

- Added `ClaudeFIndings.md` — the full code review with findings, proposed solutions, and implementation status.
- Brought `CLAUDE.md`, the per-project `Readme.md` files, the root `README.md` (version badge, packaging description), and `BuildScripts/README.md` in line with the current code and build scripts.
- NuGet packages now link to this file via `PackageReleaseNotes`.

## v2.2.1-beta - 2026-05-27

- Fixed publishing for the MAUI library (`Publish.ps1` builds and publishes the Windows target framework).
- Removed WinUI per-language `.mui` resource folders from the demo app output, keeping only `en-us`.
- Version bumped to 2.2.1 (`-beta` NuGet postfix).

## v2.2.0 - 2026-05-09

- Added MAUI support: the new `armat.localization.maui` library brings `LocalizableResourceDictionary` to MAUI applications on Android / iOS / Mac Catalyst / Windows, with a complete MAUI demo application.
- The Localization Designer recognizes and edits MAUI resource dictionaries.
- Unified versioning and packaging across all projects via the shared `Localization.Import.csproj`.
- Fixed the Designer's detection of the translations directory.

<!-- Earlier releases (v2.0.x, v1.2.x) predate this file; see the git tags for details. -->
