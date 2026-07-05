# Claude Findings — Armat.Localization code review

This document lists every finding from a full review of the library source (Core, WPF, MAUI, Designer, Demos), each with one or more proposed solutions. It is written so that each finding can be fixed independently, without any other context.

## How to work on a finding

1. **Read the whole file(s) named in the finding before editing.** Line numbers refer to commit `6858b0b`; if they have drifted, locate the code by searching for the quoted snippets — they are exact copies.
2. **Follow the repo code style** (full guide in `CONTRIBUTING.md`):
   - Tabs for indentation, Allman braces (every `{` on its own line).
   - BCL type aliases: `String`, `Int32`, `Boolean`, `Object` — never `string`, `int`, `bool`, `object`.
   - Private fields `_camelCase`, locals/params `camelCase`, types/members `PascalCase`.
   - Plain `//` comments; lowercase first letter for in-method step comments; comments explain *why*, not *what*. No `/// <summary>` on internals.
   - File-scoped namespaces; nullable reference types are enabled.
3. **Verify by building**: `dotnet build Solution/Armat.Localization/Armat.Localization.sln` from the repo root (Debug configuration; Debug uses ProjectReferences, Release pulls packages from NuGet and will not see your changes). There are no test projects — each finding has a "How to verify" section instead.
4. **One finding per commit/PR.** Commit message format: `fix(core): …`, `fix(maui): …`, `fix(designer): …`.
5. Where a finding is marked **Decision required**, implement the option marked *(Recommended)* unless a maintainer says otherwise.
6. If a fix changes public API or observable behavior, update `CLAUDE.md` and the relevant per-project `Readme.md` in the same PR (CONTRIBUTING.md requires this).

**Ratings.** Severity = how bad the consequence is in realistic use (High / Medium / Low). Confidence = how certain the finding is (Confirmed = traced and empirically verified; High = certain from code reading; Medium = mechanism certain, real-world impact depends on usage).

## Summary

| ID | Title | Severity | Confidence | Files |
|----|-------|----------|------------|-------|
| B1 | Core Uri constructors never load content | High | Confirmed | Localization.Core |
| B2 | MAUI programmatic Source paths are broken (3 ways) | High | Confirmed | Localization.Maui |
| B3 | Designer save-on-close is fire-and-forget; save path can crash the app | High | High | Localization.Designer |
| B4 | Untranslated keys are saved as `""` and blank the UI at runtime | High | High | Designer + all 3 containers |
| B5 | Editing one cell writes all locale columns and creates phantom translation files | Medium | High | Localization.Designer |
| B6 | Locale directory silently dropped for absolute file Sources | Medium | Confirmed | all 3 containers |
| B7 | Designer load/save mutex releases at the first `await` | Medium | High | Localization.Designer |
| B8 | Pre-init `Default` manager silently swallows registrations; its config has a null path | Medium | High | Localization.Core |
| B9 | Setting `Source = null` clears the dictionary, then throws | Low | High | Localization.Core |
| B10 | MAUI splits `';'` URIs backwards (latent) | Low | High | Localization.Maui |
| B11 | `CurrentLocale` updated before the load is known to succeed | Low–Med | High | all 3 containers + Designer wrapper |
| B12 | Designer MAUI wrapper leaves inner dictionary pointing at a deleted temp file | Low | Medium | Localization.Designer |
| I1–I8 | Improvements (equality semantics, robustness, perf, small cleanups) | — | — | various |

Suggested order: B1, B9, B6, B10, B11 (small independent library fixes) → B3 + B7 together (same Designer code) → B5 → B8, B12 → B2, B4 (decision required) → I1–I8.

---

## B1 — Core `LocalizableStringDictionary(Uri …)` constructors never load content

**Severity: High. Confidence: Confirmed.**
**File:** `Projects/Localization.Core/LocalizableStringDictionary.cs` (constructors at lines 23–46).

### Current code

```csharp
	public LocalizableStringDictionary(Uri source, LocalizationManager locManager)
	{
		Logger = NullLogger.Instance;

		_currLocale = LocaleInfo.Invalid;
		_loadedLocale = LocaleInfo.Invalid;

		_source = source;

		// register string dictionary in localization manager to receive further localization change events
		LocalizationManager = locManager;
	}
```

### Problem

`_source = source;` assigns the **field**, bypassing the `Source` **property** whose setter triggers `LoadNative()`. Nothing else loads the content: `EndInit()` is only invoked by XAML loading, and when the manager later fires a locale change, `LoadTranslation` throws `InvalidOperationException("Source is not initialized")` at the `if (!_isLoaded) throw` check (line 512) — an exception that `ChangeLocale` catches and only logs.

**Failure scenario:** `var dict = new LocalizableStringDictionary("/MyAsm;component/Localization/Strings.xaml");` → the dictionary is empty, stays empty forever, and every locale change logs an error. All four ctor overloads that take a source (`String` ×2, `Uri` ×2) are affected because they chain to this one.

The WPF twin does it correctly — `Projects/Localization.Wpf/LocalizableResourceDictionary.cs` line 40 assigns the `Source` **property** in its constructor.

### Fix (single option)

Change the field assignment to a property assignment so the constructor behaves exactly like the WPF one:

```csharp
		// assign through the property (not the field) so the native content is loaded;
		// the subsequent LocalizationManager registration then applies the active translation
		Source = source;
```

i.e. replace the line `_source = source;` with `Source = source;`. Order matters: keep it **before** `LocalizationManager = locManager;` so the native content exists when registration triggers a translation load.

Note the behavior change: a bad/missing source now throws from the constructor instead of failing silently later. That is desirable; mention it in the commit message.

### How to verify

Build, then run the Designer (it constructs dictionaries via `LoadNative(uri, manager)` and must keep working). Additionally, in `Projects/Demo/ClassLibrary` temporarily replace the `LoadNative` call in `Localization/StringDictionary.xaml.cs` with `new LocalizableStringDictionary(<same uri>, LocalizationManager.Default)` and confirm the WPF demo shows translated strings; revert the temporary change afterwards.

---

## B2 — MAUI programmatic `Source` paths are broken in three ways

**Severity: High. Confidence: Confirmed** (the throw message `"Source can only be set from XAML"` was verified to exist in the shipped `Microsoft.Maui.Controls.dll`; the absence of `ISupportInitialize` in that assembly was verified; relative-`Uri` value equality was verified on .NET 10).
**File:** `Projects/Localization.Maui/LocalizableResourceDictionary.cs`.

### Background fact

MAUI's `ResourceDictionary.Source` setter is (paraphrased from MAUI source):

```csharp
set
{
	if (_source == value)
		return;                       // equal value: silently does nothing
	throw new InvalidOperationException("Source can only be set from XAML.");
}
```

MAUI's `ResourceDictionary` also does **not** implement `ISupportInitialize`.

### Problem (three symptoms)

1. **Constructors throw.** Lines 44–51: `Source = source;` with a different value → `InvalidOperationException`. `new LocalizableResourceDictionary(uri)` can never work.
2. **`LoadNative()` is a silent no-op.** Lines 346–354:

```csharp
	public void LoadNative()
	{
		Uri? uri = Source;
		if (uri != null)
			Source = new Uri(uri.OriginalString, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);

		// reset the loaded locale if the native file is being loaded
		_loadedLocale = LocaleInfo.Invalid;
	}
```

   The new `Uri` is **equal** to the old one (Uri equality is by value), so the MAUI setter's equality check returns without reloading. Consequence: `LocalizationManager.ChangeLocale(LocaleInfo.Invalid)` ("switch back to native") marks the dictionary as native while the previously loaded **translated values remain**.
3. **`LoadNative(Uri, LocalizationManager)` throws `InvalidCastException`.** Lines 355–364 cast `this` to `ISupportInitialize`, which the MAUI base class does not implement.

### Fix

**Part 1 (do unconditionally) — restore native contents from a snapshot.**

Add a field near the other private fields:

```csharp
	// snapshot of the native (untranslated) contents, captured after MAUI finishes
	// loading the XAML; used to restore native values because MAUI does not allow
	// re-assigning ResourceDictionary.Source from code
	private Dictionary<String, Object>? _nativeContents = null;
```

In `OnNativeValuesLoaded` (lines 56–88), immediately after the line `((IResourceDictionary)this).ValuesChanged -= OnNativeValuesLoaded;` and **before** the `if (_locMgr == null)` block, capture the snapshot (this runs before any translation is applied):

```csharp
		// capture the native contents before any translation is applied
		_nativeContents = new Dictionary<String, Object>(this, StringComparer.Ordinal);
```

Replace the body of `LoadNative()` with:

```csharp
	public void LoadNative()
	{
		// MAUI does not allow re-assigning ResourceDictionary.Source from code
		// ("Source can only be set from XAML"), so native values are restored
		// from the snapshot captured when the dictionary was first loaded
		if (_nativeContents != null)
		{
			foreach (KeyValuePair<String, Object> pair in _nativeContents)
				this[pair.Key] = pair.Value;

			// remove keys that are not part of the native contents
			String[] extraKeys = Keys.Where(key => !_nativeContents.ContainsKey(key)).ToArray();
			foreach (String key in extraKeys)
				Remove(key);
		}

		// reset the loaded locale if the native file is being loaded
		_loadedLocale = LocaleInfo.Invalid;
	}
```

(`System.Linq` and `System.Collections.Generic` are already imported in this file.)

**Part 2 (Decision required) — the Uri constructors and `LoadNative(Uri, LocalizationManager)`.**

- **Option A *(Recommended — no in-repo consumer exists)*: fail fast with a clear message.** Replace the bodies of `LocalizableResourceDictionary(Uri source, LocalizationManager locManager)` and `LoadNative(Uri sourceUri, LocalizationManager localizationManager)` with:

```csharp
		throw new NotSupportedException(
			"MAUI does not allow assigning ResourceDictionary.Source from code. " +
			"Instantiate the dictionary from XAML with a Source attribute instead.");
```

  Keep the `String`/`Uri`-only ctor overloads chaining as they are (they will hit the same throw). Document the limitation in `Projects/Localization.Maui/Readme.md` and `CLAUDE.md`.

- **Option B: make them work by loading the XAML manually.** Store the URI in a new private field (e.g. `_programmaticSource`), read the file (`File.ReadAllText` for absolute paths, `FileSystem.OpenAppPackageFileAsync` for relative asset paths), call `this.LoadFromXaml(xamlText)`, capture the native snapshot, then run the same registration/translation logic as `OnNativeValuesLoaded`. `ResourceFilePath` must then fall back to `_programmaticSource` when the base `Source` is null. This is more work and needs manual verification that `LoadFromXaml` tolerates the file's root `x:Class` attribute — only choose it if programmatic MAUI loading is actually needed.

### How to verify

Run the MAUI demo (`Projects/Demo/MauiApp`, Windows TFM). In `AppShell.xaml.cs`, temporarily allow selecting the invalid locale (remove the `value.Culture != null` guard) and add `LocaleInfo.Invalid` to the picker source, switch to a translation (hy/ru) and back to native — the native English strings must reappear.

---

## B3 — Designer: save-on-close races process exit; save errors crash the app

**Severity: High. Confidence: High.**
**File:** `Projects/Localization.Designer/MainWindow.xaml.cs`.

### Current code

```csharp
	private void OnWindowClosing(Object sender, System.ComponentModel.CancelEventArgs e)
	{
		SaveTranslationsTable();
	}
```

(lines 317–320), which calls:

```csharp
	private async void SaveTranslationsTable(Boolean saveChangesOnly)
	{
		_ = await SaveTranslationsTableAsync(LocalizableFiles, TranslationsTable, saveChangesOnly, CancellationToken.None).ConfigureAwait(true);
	}
```

(lines 648–651).

### Problem

1. `SaveTranslationsTable()` is `async void`: `OnWindowClosing` returns at the first `await`, the window closes, WPF shuts down, and the background `Task.Run` doing the file writes races process exit → **unsaved translations can be lost on close**.
2. The entire save pipeline has no `try/catch` (unlike the load pipeline). Any IO error (locked file, read-only directory) surfaces inside an `async void` method → unhandled dispatcher exception → **application crash**.

### Fix

**Step 1.** Make the close path synchronous. The worker `SaveTranslationsTableAsyncUnsafe(...)` (line 675) is already a fully synchronous method — call it directly on the UI thread while holding the lock:

```csharp
	private void OnWindowClosing(Object sender, System.ComponentModel.CancelEventArgs e)
	{
		// save synchronously - the application may exit before a fire-and-forget
		// background save gets a chance to finish
		try
		{
			if (_fileLoadSaveMutex.WaitOne())
			{
				try
				{
					SaveTranslationsTableAsyncUnsafe(LocalizableFiles, TranslationsTable, true, CancellationToken.None);
				}
				finally
				{
					_fileLoadSaveMutex.ReleaseMutex();
				}
			}

			TranslationsTable.AcceptChanges();
		}
		catch (Exception ex)
		{
			ReportFailure(ex);
		}
	}
```

**Step 2.** Wrap the `async void` body so no other call site can crash the app:

```csharp
	private async void SaveTranslationsTable(Boolean saveChangesOnly)
	{
		try
		{
			_ = await SaveTranslationsTableAsync(LocalizableFiles, TranslationsTable, saveChangesOnly, CancellationToken.None).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			ReportFailure(ex);
		}
	}
```

**Coordinate with B7** — if B7 replaces `_fileLoadSaveMutex` with a `SemaphoreSlim`, use `_fileLoadSaveSemaphore.Wait()` / `Release()` here instead of `WaitOne()` / `ReleaseMutex()`. Do B7 first or in the same PR.

### How to verify

Run the Designer, load `Projects/Localization.Designer/Localization/UIMessagesSD.xaml`, edit a translation cell, immediately close the window, reopen and confirm the edit persisted. Then make one translation file read-only, edit + close, and confirm an error dialog appears instead of a crash.

---

## B4 — Untranslated keys are persisted as `""`, which blanks the UI at runtime

**Severity: High. Confidence: High** (mechanism traced end-to-end; possibly a deliberate Designer editing model whose on-disk consequence was not intended). **Decision required.**
**Files:** `Projects/Localization.Designer/LocalizableResourceFile.cs` (line 116), `Projects/Localization.Designer/MainWindow.xaml.cs` (line 720), and the `LoadTranslation` loops of all three containers.

### Problem chain

1. The Designer loads translations with `TranslationLoadBehavior.ClearNative` (`LocalizableResourceFile.cs` line 116) → every key missing from a translation file becomes `""` in the in-memory dictionary.
2. On save, `dataRow.IsNull(column) ? String.Empty : (String)dataRow[column]` (`MainWindow.xaml.cs` line 720) materializes empty cells as `""`, and `SaveTranslation()` serializes the **entire** dictionary.
3. Result: a partially translated `.trd`/`.tsd` contains *every* key, the untranslated ones with empty values.
4. At application runtime the default `KeepNative` behavior only preserves native text for keys **missing from the file**. These keys are present (with `""`), so users see **blank labels** wherever a translation wasn't provided.

### Fix options

- **Option 1 *(Recommended)* — treat empty values as "not translated" when loading.** This fixes already-shipped files too and keeps the Designer UX identical (empty cells still show empty via `ClearNative`).

  In `Projects/Localization.Core/LocalizableStringDictionary.cs`, `LoadTranslation(FileInfo, TranslationLoadBehavior)` (loop at lines 527–533), change:

```csharp
				foreach (TextRecord record in docDeserialized.Records)
				{
					// ensure to replace only existing keys, do not add new ones
					// treat empty values as "not translated" so TranslationLoadBehavior
					// decides what happens to them (KeepNative keeps the native text)
					if (String.IsNullOrEmpty(record.Key) || String.IsNullOrEmpty(record.Value))
						continue;

					if (unusedKeys.Remove(record.Key))
						this[record.Key] = record.Value;
				}
```

  In `Projects/Localization.Wpf/LocalizableResourceDictionary.cs`, `LoadTranslation(FileInfo, TranslationLoadBehavior)` (loop at lines 365–371):

```csharp
					foreach (Object key in dicLocalized.Keys)
					{
						// treat empty string values as "not translated"
						if (dicLocalized[key] is String strValue && strValue.Length == 0)
							continue;

						// ensure to replace only existing keys, do not add new ones
						if (unusedKeys.Remove(key))
							this[key] = dicLocalized[key];
					}
```

  In `Projects/Localization.Maui/LocalizableResourceDictionary.cs`, `LoadTranslation(Stream, TranslationLoadBehavior)` (loop at lines 444–449), apply the same skip for `pair.Value is String s && s.Length == 0`.

  Behavior change to document: an intentionally-empty translation is no longer representable — an empty value now means "fall back per `TranslationLoadBehavior`".

- **Option 2 (optional, additive) — stop writing empty values.** In the three `SaveTranslation` implementations, skip entries whose value is an empty `String` before serializing. Keeps files clean going forward; does not fix existing files. Note the Designer round-trip still shows untranslated cells as empty because `ClearNative` blanks missing keys on load.

Implementing Option 1 alone is sufficient; Option 1 + 2 is cleanest.

### How to verify

Create a translation file containing only *some* keys with values and at least one key with `Value=""`. Run the WPF demo with that locale: keys absent or empty in the file must show native text (with default `KeepNative`), not blanks. Then open the file in the Designer and confirm untranslated cells still display as empty.

---

## B5 — Designer: editing one cell writes all locale columns and creates phantom translation files

**Severity: Medium. Confidence: High.**
**File:** `Projects/Localization.Designer/MainWindow.xaml.cs`, `SaveTranslationsTableAsyncUnsafe` (lines 675–741).

### Problem

The save loop iterates **every locale column** for **every changed row** — not changed cells (a per-cell check exists but is commented out at lines 715–716). Locale columns are the union of locales across *all* loaded files. Consequences:

1. Editing one French cell rewrites that file's translation files for every locale column.
2. Worse: if file X has a changed row and the table has an `hy` column only because *another* file has Armenian, `SaveTranslations(hy, …)` → `CreateTranslation` **creates** `hy/X.trd` for file X — a translation file the project never had, populated with all-empty values (see B4).

### Fix

**Step 1 — only save cells that actually changed** (restores the intent of the commented-out check, with null-safety). Inside the `foreach (DataRow dataRow in contentsToSave.Rows)` loop, right after the cancellation check and before reading `localizableFileId`, add:

```csharp
				// a changed row carries current values for ALL locale columns, not just
				// the edited one - skip cells whose value did not actually change
				if (saveChangesOnly && dataRow.HasVersion(DataRowVersion.Original))
				{
					Object currValue = dataRow[column];
					Object origValue = dataRow[column, DataRowVersion.Original];
					if (Equals(currValue, origValue))
						continue;
				}
```

(`saveChangesOnly` must be threaded into scope — it already is a parameter of `SaveTranslationsTableAsyncUnsafe`.)

**Step 2 — never create files for locales a file does not have.** In the per-file save loop (lines 729–737), after resolving `file`, add:

```csharp
				// do not create translation files for locales this file never had;
				// adding a language goes through the explicit AddLanguage command
				LocalizationManager? fileManager = file.LocalizationManager;
				if (fileManager != null && !fileManager.AllLocales.Contains(locale))
					continue;
```

Note: the explicit "Add Language" toolbar command calls `CreateTranslation` for every loaded file first, so after adding a language the locale exists for each file and this guard does not block legitimate saves. (If I1 — name-based `LocaleInfo` equality — is implemented, `Contains` works even more reliably; without I1 it still works because both sides are constructed without `DisplayNameOverride`.)

### How to verify

Load two localizable files where only one has a `ru` translation directory. Edit a cell of the *other* file and save (Ctrl+S / Save All / close). Confirm no `ru/` directory or `.trd`/`.tsd` file appears next to the file that never had Russian, and that only the edited locale's file got rewritten (check file modification times).

---

## B6 — Locale directory silently dropped for absolute file Sources

**Severity: Medium. Confidence: Confirmed** (`Path.Combine('C:\\app\\Localization\\en', 'D:\\proj\\File.xaml')` returns the second argument — verified).
**Files / methods (same pattern three times):**
- `Projects/Localization.Core/LocalizableStringDictionary.cs`, `GetTranslationFilePath(String, LocaleInfo)`, combine at lines 311–315
- `Projects/Localization.Wpf/LocalizableResourceDictionary.cs`, same method, combine at lines 215–219
- `Projects/Localization.Maui/LocalizableResourceDictionary.cs`, same method, combine at lines 276–280

### Problem

When `Source` is an absolute file path (e.g. `D:\app\Data\File.xaml`) and `Configuration.TranslationsDirectoryPath` is relative (default `"Localization"`), the root-strip `StartsWith(rootPath + Path.DirectorySeparatorChar)` does not match, so `xamlFileName` stays absolute. `Path.Combine(localeDir.FullName, xamlFileName)` then returns `xamlFileName` **unchanged** (documented `Path.Combine` behavior for rooted second arguments). After the extension swap, `GetTranslationFilePath("en")` and `GetTranslationFilePath("fr")` both yield `D:\app\Data\File.tsd` — the locale segment vanished. Loads read the wrong file; saves for different locales overwrite each other.

The Designer is unaffected because it always passes an **absolute** `TranslationsDirectoryPath`, which makes the strip succeed.

### Fix

In each of the three methods, immediately **before** the `xamlFileName = Path.Combine(localeDir.FullName, xamlFileName);` line, insert:

```csharp
			// an absolute path would make Path.Combine return it unchanged and
			// silently drop the locale directory - keep only the file name instead
			if (Path.IsPathRooted(xamlFileName))
				xamlFileName = Path.GetFileName(xamlFileName);
```

Resulting semantics: for absolute sources the translation is looked up at `<TranslationsDirectory>/<locale>/<FileName>.<ext>` — consistent with the documented layout. (Alternative considered: throwing an exception; rejected because a well-defined lookup location is more useful.)

### How to verify

Console app snippet: create `LocalizationManager` with default config, create a `LocalizableStringDictionary`, call `LoadNative(new Uri(@"D:\tmp\Strings.xaml"), manager)` against a real file, then check `GetTranslationFilePath(new LocaleInfo("en"))` ends with `Localization\en\Strings.tsd` and differs from the `fr` result.

---

## B7 — Designer: the load/save mutex releases at the first `await`

**Severity: Medium. Confidence: High** (mechanics certain; user-visible corruption needs unlucky timing).
**File:** `Projects/Localization.Designer/MainWindow.xaml.cs`, lines 486–541 (load) and 652–674 (save).

### Current code (load side)

```csharp
	private static async Task<TranslationsTable?> LoadTranslationsTableAsync(IEnumerable<LocalizableResourceFile> selectedFiles,
		IEnumerable<LocalizableResourceFile> allFiles, TranslationsTable? currentTable, CancellationToken token)
	{
		return await Task.Run(() =>
		{
			if (!_fileLoadSaveMutex.WaitOne())
				return null;

			try
			{
				return LoadTranslationsTableAsyncUnsafe(selectedFiles, allFiles, currentTable, token);
			}
			finally
			{
				_fileLoadSaveMutex.ReleaseMutex();
			}
		}).ConfigureAwait(true);
	}
```

`LoadTranslationsTableAsyncUnsafe` (line 542) is an `async` method whose first statement is `await SaveTranslationsTableAsync(...).ConfigureAwait(false)`.

### Problem

The lambda calls the `async` worker, which returns its `Task` at the **first `await`**; the `finally` then releases the mutex immediately. The entire remaining load (pending-changes save + table fill, which reads translation files and mutates each `LocalizableResourceFile`'s shared `LocalizableResourceTranslations` cache) runs **without the lock**. The design avoids deadlock only by accident (the nested save's `WaitOne` blocks until the outer `finally` runs). Rapid selection changes can overlap a save with another load's cache mutations → `Dictionary` corruption or spurious exceptions. Additionally, `SaveTranslationsTableAsync`'s `table.AcceptChanges()` — commented as "must run on the UI thread" — runs on a thread-pool thread when invoked from inside the load worker (`ConfigureAwait(false)` context).

### Fix (restructure; do together with B3)

1. Replace the field (line 487):

```csharp
	private static readonly SemaphoreSlim _fileLoadSaveSemaphore = new(1, 1);
```

2. Make the workers fully synchronous. Rename `LoadTranslationsTableAsyncUnsafe` → `LoadTranslationsTableUnsafe`, remove `async`/`await` from it, and replace its first statement with a **direct call** to the synchronous save worker (no lock acquisition — the caller already holds the semaphore; `SemaphoreSlim` is not reentrant, a nested `Wait` would deadlock):

```csharp
		// check if there are any changes to apply back
		if (currentTable != null)
			SaveTranslationsTableAsyncUnsafe(allFiles, currentTable, true, token);
```

   (Skipping the old table's `AcceptChanges` here is fine: the table is replaced right after; if the load is canceled the changes remain flagged and the next save re-saves them, which is idempotent.)

3. Acquire the semaphore in the two entry points, around the `Task.Run`:

```csharp
	private static async Task<TranslationsTable?> LoadTranslationsTableAsync(IEnumerable<LocalizableResourceFile> selectedFiles,
		IEnumerable<LocalizableResourceFile> allFiles, TranslationsTable? currentTable, CancellationToken token)
	{
		await _fileLoadSaveSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(true);
		try
		{
			return await Task.Run(() => LoadTranslationsTableUnsafe(selectedFiles, allFiles, currentTable, token)).ConfigureAwait(true);
		}
		finally
		{
			_fileLoadSaveSemaphore.Release();
		}
	}
```

   and equivalently in `SaveTranslationsTableAsync`, keeping its `table.AcceptChanges()` **after** the `finally` block on the UI thread (the `ConfigureAwait(true)` continuation).

4. Update `OnWindowClosing` (from B3) to use `_fileLoadSaveSemaphore.Wait()` / `.Release()`.

### How to verify

Build; run the Designer; click rapidly between several loaded files while a large file set is loading — no exceptions, table always ends on the last selected file. Verify saving still works (edit + Save All + reload).

---

## B8 — Pre-init `Default` manager silently swallows registrations; `_none` has a null translations path

**Severity: Medium. Confidence: High.**
**File:** `Projects/Localization.Core/LocalizationManager.cs`.

### Problem

1. `LocalizationManager.Default` returns a placeholder `_none` instance before `CreateDefaultInstance` is called (lines 74–79). Any localizable dictionary created earlier registers with `_none`, and because the containers forbid re-assigning `LocalizationManager` ("Cannot reset the localization manager"), it **never** receives locale changes from the real default manager created later. There is no error anywhere — the symptom is "this dictionary never translates". (Example hazard: a MAUI `App.xaml` merged dictionary initializes during `InitializeComponent()`, *before* `CreateDefaultInstance` in the `App` constructor.)
2. `_none` is constructed by the private ctor (lines 16–22) which never sets `Configuration`, so it holds `default(Configuration)` — the explicit parameterless struct constructor is **bypassed** — making `TranslationsDirectoryPath` **null** despite its non-nullable declaration. If `ChangeLocale` is ever called on `_none` with a registered target, `GetTranslationFilePath` dereferences it → `NullReferenceException`.
3. `CLAUDE.md` states `CreateDefaultInstance` "can only be called once **before anyone reads `Default`**" — the code does not enforce the "before anyone reads" part; reading `Default` first silently splits the world.

### Fix

**Step 1** — give `_none` sane configuration. In the private constructor add as the first statement:

```csharp
		Configuration = Configuration.Default;
```

**Step 2** — surface the misconfiguration. In `CreateDefaultInstance(Configuration, ILoggerFactory)` (lines 36–46), after `_default = CreateInstance(config, loggerFactory);` add:

```csharp
		// targets created before this call registered with the placeholder manager
		// and will never receive locale changes from the default manager - make the
		// misconfiguration visible instead of failing silently
		if (_none._listTargets.Count > 0)
			_default.Logger.LogWarning("{Count} localization target(s) were registered before CreateDefaultInstance was called; they will not follow the default manager's locale changes", _none._listTargets.Count);
```

`Logger` is `protected`; since this is a static member of the same class, access is allowed.

**Step 3** — fix the `CLAUDE.md` sentence to match reality, e.g.: "`CreateDefaultInstance(...)` populates `LocalizationManager.Default` and throws if called twice. Reading `Default` before it is called returns a placeholder manager; targets registered with the placeholder never migrate — create the default manager before constructing any localizable resources (a warning is logged otherwise)."

(A deeper fix — allowing containers to upgrade from the placeholder to the real default — requires relaxing the "Cannot reset the localization manager" guard in all three containers and is intentionally out of scope here.)

### How to verify

In a scratch console app referencing Core: create a `LocalizableStringDictionary` (triggering registration via `LocalizationManager.Default`), then call `CreateDefaultInstance` with a console logger factory → the warning must appear. Also call `ChangeLocale("en")` on `Default` *before* `CreateDefaultInstance` with a registered target → no `NullReferenceException` after Step 1.

---

## B9 — Core: setting `Source = null` clears the dictionary, then throws

**Severity: Low. Confidence: High.**
**File:** `Projects/Localization.Core/LocalizableStringDictionary.cs`, property at lines 92–106.

### Current code

```csharp
		set
		{
			_source = value;

			// try to auto-load
			if (_source == null || ResourceFilePath.Length > 0)
				LoadNative();
		}
```

### Problem

`dict.Source = null` routes into `LoadNative()`, which first `Clear()`s the dictionary and then throws `InvalidOperationException("Source is not initialized")` (line 414). Destructive side effect + exception for what reads like a reset.

### Fix

```csharp
		set
		{
			_source = value;

			// a null source unloads the dictionary contents
			if (_source == null)
			{
				Clear();
				_isLoaded = false;
				_loadedLocale = LocaleInfo.Invalid;
				return;
			}

			// try to auto-load
			if (ResourceFilePath.Length > 0)
				LoadNative();
		}
```

### How to verify

Build; scratch snippet: load a dictionary, set `Source = null` → no exception, `Count == 0`; set a valid `Source` again → content loads.

---

## B10 — MAUI splits `';'` URIs backwards (latent)

**Severity: Low (latent — MAUI source URIs normally contain no `';'`). Confidence: High.**
**File:** `Projects/Localization.Maui/LocalizableResourceDictionary.cs`, `GetTranslationAssetPath` (lines 212–217) and `GetTranslationFilePath` (lines 260–265).

### Current code (both places)

```csharp
		Int32 pathSepIndex = xamlFileName.LastIndexOf(';');
		if (pathSepIndex != -1)
			xamlFileName = xamlFileName[..pathSepIndex];
		if (xamlFileName.StartsWith("component/"))
			xamlFileName = xamlFileName.Remove(0, "component/".Length);
```

### Problem

For `;component/`-style URIs (`/MyAsm;component/Localization/File.xaml`), `[..pathSepIndex]` keeps the **assembly** part (`/MyAsm`) and discards the path. The WPF implementation correctly keeps what comes **after** the separator (`Projects/Localization.Wpf/LocalizableResourceDictionary.cs` line 202: `xamlFileName[(pathSepIndex + 1)..]`). The dead `StartsWith("component/")` line right below shows the intended behavior.

### Fix

In both MAUI methods change the slice to match WPF:

```csharp
		if (pathSepIndex != -1)
			xamlFileName = xamlFileName[(pathSepIndex + 1)..];
```

### How to verify

Build; scratch check: `GetTranslationAssetPath("/MyAsm;component/Localization/File.xaml", new LocaleInfo("en"))` must return `Localization/en/File.trd` (it currently throws / returns garbage). Regular MAUI URIs without `';'` are unaffected.

---

## B11 — `CurrentLocale` is updated before the load is known to succeed

**Severity: Low–Medium. Confidence: High.**
**Files / lines (same pattern):**
- `Projects/Localization.Core/LocalizableStringDictionary.cs` — `_currLocale = locale;` at line 481, success point after `LoadTranslation(locFileInfo, loadBehavior)` at line 505
- `Projects/Localization.Wpf/LocalizableResourceDictionary.cs` — line 323 / line 346
- `Projects/Localization.Maui/LocalizableResourceDictionary.cs` — line 384 / line 428
- `Projects/Localization.Designer/LocalizableMauiResourceDictionary.cs` — line 115 (same pattern)

### Problem

`LoadTranslation(LocaleInfo)` assigns `_currLocale = locale` **before** checking that the translation file exists. When the file is missing (returns `false`, `KeepNative` keeps native content), `CurrentLocale` reports the new locale while the content is native. A subsequent `SaveTranslation()` then writes the **native** values into that locale's translation file.

### Fix

In all four methods, move the `_currLocale = locale;` assignment to **after** the successful load, next to the `_loadedLocale` assignment. Example for Core (lines 480–507):

```csharp
		String xamlFileName = GetTranslationFilePath(locale);
		...
		// load from translation file
		LoadTranslation(locFileInfo, loadBehavior);

		// update the locale fields only after the translation has actually loaded
		_currLocale = locale;
		_loadedLocale = locale;

		return true;
```

Remove the early assignment. Notes:
- `GetTranslationFilePath(locale)` takes the locale as a parameter and does not depend on `_currLocale` — safe to reorder.
- After this change, when a translation file is missing, `OnLocalizationChanged`'s "Localization change didn't happen" warning fires — which is now *accurate*.
- In the Designer wrapper (`LocalizableMauiResourceDictionary`), `SaveTranslation` uses `_currLocale`; the Designer always calls `CreateTranslation` before saving, so `LoadTranslation` succeeds and `_currLocale` is set — the flow keeps working.

### How to verify

Scratch: load a dictionary, call `LoadTranslation(new LocaleInfo("de"))` where no `de` file exists → returns `false` and `CurrentLocale` still reports the previous locale. Designer smoke test: edit + save translations still lands in the correct locale files.

---

## B12 — Designer MAUI wrapper: inner dictionary points at a deleted temp file

**Severity: Low (unreachable through the current UI; a landmine for future changes). Confidence: Medium.**
**File:** `Projects/Localization.Designer/LocalizableMauiResourceDictionary.cs`, `LoadNative` (lines 85–102).

### Problem

`LoadNative` converts the MAUI XAML to a temp WPF file, hands it to `_innerDictionary.LoadNative(tempUri, localizationManager)` — which registers the inner dictionary with the per-file manager and leaves its `Source` pointing at the temp path — and then **deletes the temp file** in `finally`. If anything ever calls `ChangeLocale` on that per-file manager, the inner dictionary's own path logic resolves against the deleted temp path: a native reload fails, and a valid-locale load resolves to a nonexistent file and (with the wrapper's configured `ClearNative`) wipes all values.

### Fix *(Recommended)* — keep the temp native file alive for the wrapper's lifetime

1. Add a field `private String? _tempNativePath;` and make the class implement `IDisposable`.
2. In `LoadNative`, delete any previous `_tempNativePath`, create the new temp file, convert into it, load the inner dictionary from it, and **do not delete it** (remove the `finally { TryDeleteFile(tempPath); }` for this method only; translation/save temp files at lines 128–151, 170–179, 199–212 stay short-lived and keep their `finally` deletes).
3. `Dispose()` calls `TryDeleteFile(_tempNativePath)`.
4. In `Projects/Localization.Designer/LocalizableResourceFile.cs`, dispose the previous resource when resetting — in `Reset()` (lines 89–99) add before nulling:

```csharp
		(LocalizableResource as IDisposable)?.Dispose();
```

Leaked temp files on abnormal exit are in `%TEMP%` and harmless.

### How to verify

Designer smoke test: scan `Projects/Demo/MauiApp`, open the MAUI files, edit + save translations, remove/re-add files repeatedly — no errors, and `%TEMP%` does not accumulate `.xaml` files during a session beyond one per loaded MAUI file.

---

# Improvements

## I1 — `LocaleInfo` equality includes `DisplayNameOverride`; `CompareTo` is inconsistent

**File:** `Projects/Localization.Core/LocaleInfo.cs`.
`LocaleInfo` is a `record`, so synthesized equality compares `Culture` **and** `DisplayNameOverride`. Two infos for the same culture with different display overrides compare unequal — breaking picker-selection vs `CurrentLocale` comparisons, `ChangeLocale`'s early-return, and `Contains` checks. `CompareTo` (by `Name`, culture-sensitive) disagrees with `Equals`.

**Fix:** define equality by culture name (records allow replacing the synthesized members):

```csharp
	// equality is based on the culture name only - two LocaleInfo instances for the
	// same culture must compare equal regardless of DisplayNameOverride
	public virtual Boolean Equals(LocaleInfo? other)
	{
		if (other is null)
			return false;

		return Name == other.Name;
	}
	public override Int32 GetHashCode()
	{
		return Name.GetHashCode();
	}
```

and make `CompareTo` ordinal: `return String.CompareOrdinal(Name, other.Name);`. Behavior note: `LocaleInfo.Invalid` (`Name == ""`) becomes equal to any other culture-less instance, e.g. the Designer's "[no selection]" item (`InvariantCulture`, `Name == ""`) — check `MainWindow.SupportedLocales` (Designer) still renders both entries; if the `Union` there collapses them, keep only the invalid entry.

## I2 — `AllLocales` asymmetry: `DefaultLocale` prepended only in scan mode

**File:** `Projects/Localization.Core/LocalizationManager.cs`, lines 88–127. The directory-scan branch prepends `Configuration.DefaultLocale` when missing; the `SupportedLocales` branch (lines 93–94) returns the list as-is. Either unify (prepend in both branches — note UIs bound to `AllLocales` may then show a "[Native]" entry, a visible behavior change) or document the asymmetry in `Projects/Localization.Core/Readme.md` and `CLAUDE.md`. *(Recommended: document only.)*

## I3 — Use `AppContext.BaseDirectory` for relative translation paths

**File:** `Projects/Localization.Core/LocalizationManager.cs`, lines 201–207. `Assembly.GetEntryAssembly()?.Location` is an empty string in single-file published apps, so relative paths silently resolve against the current working directory. Replace the block with:

```csharp
		if (!Path.IsPathFullyQualified(locDirectoryPath))
		{
			// combine the relative path with the application base directory
			// (Assembly.Location is empty in single-file published apps)
			locDirectoryPath = Path.Combine(AppContext.BaseDirectory, locDirectoryPath);
		}
```

## I4 — Thread-safety of `CreateDefaultInstance`

**File:** `Projects/Localization.Core/LocalizationManager.cs`, lines 36–46. The check-then-set on `_default` races. Guard with a private static lock object around the check and assignment. Also document (in `CLAUDE.md` / Core Readme) that `ChangeLocale` and target registration are expected to run on a single (UI) thread.

## I5 — Harden the locale directory scan for ICU platforms

**File:** `Projects/Localization.Core/LocalizationManager.cs`, `GetLocaleInfoFromDirectory` (lines 223–241). On Windows/.NET 10, `CultureInfo.GetCultureInfo("NestedRD")` throws (verified), so junk directories are already skipped — but ICU on Linux fabricates cultures for well-formed unknown names, which would surface junk directories as locales. Inside the existing `try`, construct via:

```csharp
			result = new LocaleInfo(CultureInfo.GetCultureInfo(locDirInfo.Name, predefinedOnly: true));
```

(the existing `catch` already handles `CultureNotFoundException`). Do **not** apply `predefinedOnly` to the public `LocaleInfo(String)` constructor — it would reject legitimate user-registered custom cultures.

## I6 — MAUI asset probe: don't swallow silently

**File:** `Projects/Localization.Maui/LocalizableResourceDictionary.cs`, lines 391–399. The `catch { }` around `AppPackageFileExistsAsync`/`OpenAppPackageFileAsync` hides real failures. Change to `catch (Exception ex)` and add `Logger.LogDebug(ex, "Failed to open translation asset {assetPath}", assetPath);`. (The sync-over-async `GetAwaiter().GetResult()` on the UI thread is acknowledged; changing it requires an async API on `ILocalizableResource` — out of scope.)

## I7 — Small cleanups (one commit, `fix(core): assorted robustness cleanups` or split as convenient)

1. **Dead null-check:** `Projects/Localization.Core/LocalizableStringDictionary.cs` lines 234–239 and 243–248 — `Assembly.Load` / `GetEntryAssembly` null-checks: `Assembly.Load` never returns null (it throws). Replace with a `try/catch` that rethrows a `TypeLoadException` with the assembly name, or simply delete the dead branch.
2. **Mutation while enumerating:** same file, lines 476 and 497 pass the **live** `Keys` collection into `ResetTranslationForKeys`, which mutates the dictionary while iterating. Works on modern .NET but is undefined contract. Snapshot first — inside `ResetTranslationForKeys` (lines 544–556), start with `String[] keyArray = keys.ToArray();` and iterate `keyArray` (this mirrors what the WPF version at Wpf line 384 and the MAUI version at Maui line 462 already do).
3. **Redundant cast:** same file, line 521: `Keys.OfType<String>().ToHashSet()` → `Keys.ToHashSet(StringComparer.Ordinal)`.
4. **Null translation values:** same file, line 531 — `TextRecord.Value` can be null after XML deserialization (`Projects/Localization.Core/LocalizableStringDictionarySerialization.cs` line 11 is a non-nullable field the serializer may leave null). Use `this[record.Key] = record.Value ?? String.Empty;` — or skip nulls entirely if B4 Option 1 is implemented (its `IsNullOrEmpty` check covers this).
5. **MessageBox from background threads:** `Projects/Localization.Designer/MainWindow.xaml.cs` lines 796–799 — `ReportFailure` is called from thread-pool code paths. Marshal: `System.Windows.Application.Current?.Dispatcher.Invoke(() => MessageBox.Show(...));`.
6. **Event re-subscription:** `Projects/Demo/WpfApp/MainWindow.xaml.cs` lines 36–43 — `PopupWindow.Closing += …` runs on every button click for an existing popup. Subscribe only when constructing: `if (PopupWindow == null) { PopupWindow = new(); PopupWindow.Closing += (s, args) => PopupWindow = null; }`.
7. **Singleton lazy-init:** `Projects/Localization.Designer/Localization/UIMessagesSD.xaml.cs` lines 18–30 — not thread-safe; fine for UI-thread-only use. Either add a comment stating that constraint or switch to `private static readonly Lazy<UIMessagesSD> _instance = new(() => new UIMessagesSD());`.
8. **XML hardening:** `Projects/Localization.Core/LocalizableStringDictionarySerialization.cs` `Load` (lines 22–28) — deserialize through an explicit reader with DTDs prohibited: `using System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream, new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit });` and pass `reader` to `serializer.Deserialize`.

## I8 — Designer: cache misses in `GetResourceTranslation`

**File:** `Projects/Localization.Designer/LocalizableResourceFile.cs`, lines 192–224. When a translation file is absent, `LoadTranslation` returns `false`, the dictionary is **not** cached, and every UI refresh re-parses the full native XAML for that locale. Add a negative cache:

```csharp
	private readonly HashSet<String> _missingTranslations = new();
```

- At the top of `GetResourceTranslation`, after the cache lookup: `if (_missingTranslations.Contains(locale.Name)) return null;`
- When `dict.LoadTranslation(locale)` returns `false`: `_missingTranslations.Add(locale.Name);` (and still return the dict, matching current behavior, or return null — callers handle null).
- Invalidate: remove the locale name in `SaveTranslations` and `CreateTranslation`; clear the set in `Reset()` and after `DeleteTranslation`.

Verify: Designer still shows empty columns for locales without files, and creating/saving a translation makes it appear without restarting.

---

# Cross-cutting notes for the implementer

- **B3 and B7 touch the same methods** — implement in one PR, B7's structure first.
- **B4 and B5 interact**: B5 stops *new* phantom empty files; B4 makes existing ones harmless. Both are worth doing.
- **B1, B6, B9, B10, B11** are independent, low-risk, and safe to do first.
- After library-behavior changes (B2, B4, B6, B11), update `CLAUDE.md` and the per-project `Readme.md` files accordingly (CONTRIBUTING.md requirement).
- All findings were made at commit `6858b0b`. Empirical verifications performed: `Path.Combine` rooted-second-argument behavior; `CultureInfo.GetCultureInfo("NestedRD")` throwing on Windows/.NET 10; presence of the `"Source can only be set from XAML"` string and absence of `ISupportInitialize` in the shipped `Microsoft.Maui.Controls.dll`; relative-`Uri` value equality.
