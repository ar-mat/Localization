using System;
using System.Collections.Generic;

namespace Armat.Localization;

public class LocalizationChangeEventArgs : EventArgs
{
	public LocalizationChangeEventArgs(LocaleInfo newLocale) : base()
	{
		OldLocale = null;
		NewLocale = newLocale;
	}
	public LocalizationChangeEventArgs(LocaleInfo? oldLocale, LocaleInfo newLocale) : base()
	{
		OldLocale = oldLocale;
		NewLocale = newLocale;
	}

	public LocaleInfo? OldLocale { get; init; }
	public LocaleInfo NewLocale { get; init; }
}

public delegate void LocalizationChangeEventHandler(object sender, LocalizationChangeEventArgs e);

public interface ILocalizationTarget
{
	LocaleInfo CurrentLocale { get; }
	void OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args);
}

public interface ILocalizableResource
{
	// Represents list of supported extensions for localizable files in native language
	public String[] NativeFileExtensions { get; }

	// Represents list of supported extensions for localizable file translations
	public String[] TranslationFileExtensions { get; }

	// returns the source URI from where it has been loaded
	Uri Source { get; }

	// Load / reload native language contents
	void LoadNative(Uri sourceUri, LocalizationManager localizationManager);

	// Loads translations for the given locale
	// Returns false if the file is not found
	Boolean LoadTranslation(LocaleInfo locale);

	// saves the current locale translation
	void SaveTranslation();

	// Creates a translation for the given locale if it doesn't exists
	void CreateTranslation(LocaleInfo locale);

	// Deletes translation for the given locale
	void DeleteTranslation(LocaleInfo locale);

	// returns collection of key -> translation for teh current locale
	IEnumerable<KeyValuePair<String, String>> Enumerate();

	// updates translations for the given entries
	void UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations);
}
