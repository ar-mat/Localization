using Armat.Localization.Wpf;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Armat.Localization.Designer;

public class LocalizableResourceFile
{
	public LocalizableResourceFile()
	{
		Id = Guid.NewGuid();
		FullPath = String.Empty;

		LocalizationManager = null;

		LocalizableResource = null;
		ResourceType = LocalizableResourceType.Unknown;
		LocalizableResourceTranslations = new Dictionary<String, ILocalizableResource>();
	}

	public Guid Id { get; init; }
	public String FullPath { get; private set; }

	public LocalizationManager? LocalizationManager { get; private set; }

	private String? _translationsDirectoryPath = null;
	public String FileName
	{
		get => System.IO.Path.GetFileName(FullPath) ?? String.Empty;
	}
	public String TranslationsDirectoryPath
	{
		get
		{
			return _translationsDirectoryPath ??
				System.IO.Path.GetDirectoryName(FullPath) ?? 
				String.Empty;
		}
		private set
		{
			if (value.Length == 0)
				_translationsDirectoryPath = null;
			else
				_translationsDirectoryPath = value;
		}
	}

	public LocalizableResourceType ResourceType
	{
		get; private set;
	}
	public String ResourceTypeImageSource
	{
		get => ResourceType switch
		{
			LocalizableResourceType.Unknown => "/Resources/LocalizableResourceType_Unknown.png",
			LocalizableResourceType.StringDictionary => "/Resources/LocalizableResourceType_SD.png",
			LocalizableResourceType.WpfResourceDictionary => "/Resources/LocalizableResourceType_WPF.png",
			LocalizableResourceType.MauiResourceDictionary => "/Resources/LocalizableResourceType_MAUI.png",
			_ => throw new InvalidOperationException()
		};
	}

	public ILocalizableResource? LocalizableResource { get; private set; }
	private Dictionary<String, ILocalizableResource> LocalizableResourceTranslations { get; set; }

	// locales known to have no translation file - avoids re-parsing the native
	// resource on every UI refresh (see GetResourceTranslation)
	private readonly HashSet<String> _missingTranslations = new();

	// Represents list of supported extensions for localizable files in native language
	private static readonly String[] _nativeFileExtArray =
		(new String[]
		{
			LocalizableStringDictionary.NativeFileExtension,
			LocalizableResourceDictionary.NativeFileExtension
		}).GroupBy(x => x).Select(grp => grp.First()).ToArray();
	public static String[] NativeFileExtensions => _nativeFileExtArray;

	// Represents list of supported extensions for localizable file translations
	// tsd = translated string dictionary
	private static readonly String[] _transFileExtArray =
		(new String[]
		{
			LocalizableStringDictionary.TranslationFileExtension,
			LocalizableResourceDictionary.TranslationFileExtension
		}).GroupBy(x => x).Select(grp => grp.First()).ToArray();
	public static String[] TranslationFileExtensions => _transFileExtArray;


	public void Reset()
	{
		// Set file path property
		FullPath = String.Empty;
		LocalizationManager = null;

		// reset loaded contents
		(LocalizableResource as IDisposable)?.Dispose();
		LocalizableResource = null;
		ResourceType = LocalizableResourceType.Unknown;
		LocalizableResourceTranslations.Clear();
		_missingTranslations.Clear();
	}
	public Boolean Load(String resourceFilePath, String? translationsDirectoryPath = null)
	{
		// reset loaded contents
		Reset();

		// Set file path property
		FullPath = resourceFilePath;
		if (translationsDirectoryPath != null)
			TranslationsDirectoryPath = translationsDirectoryPath;

		// create an isolated localization manager for this file
		// it will be used to load / save localizations for the current resource
		Configuration cfg = new()
		{
			// use full file path, so it would be possible to locate the appropriate localization files
			TranslationsDirectoryPath = TranslationsDirectoryPath,
			TranslationLoadBehavior = TranslationLoadBehavior.ClearNative
		};
		LocalizationManager = LocalizationManager.CreateInstance(cfg);

		// load native contents in either of string dictionary or resource dictionary format
		// and update the ResourceType accordingly
		if (ResourceType == LocalizableResourceType.Unknown)
		{
			LocalizableResource = TryLoadStringDictionary(resourceFilePath, LocalizationManager);
			if (LocalizableResource != null)
				ResourceType = LocalizableResourceType.StringDictionary;
		}
		if (ResourceType == LocalizableResourceType.Unknown)
		{
			LocalizableResource = TryLoadWpfResourceDictionary(resourceFilePath, LocalizationManager);
			if (LocalizableResource != null)
				ResourceType = LocalizableResourceType.WpfResourceDictionary;
		}
		if (ResourceType == LocalizableResourceType.Unknown)
		{
			LocalizableResource = TryLoadMauiResourceDictionary(resourceFilePath, LocalizationManager);
			if (LocalizableResource != null)
				ResourceType = LocalizableResourceType.MauiResourceDictionary;
		}

		return ResourceType != LocalizableResourceType.Unknown;
	}
	private static ILocalizableResource? TryLoadStringDictionary(String filePath, LocalizationManager localizationManager)
	{
		Uri resourceUri = new(filePath, UriKind.Absolute);

		// instantiate the localizable string dictionary
		LocalizableStringDictionary dict = new();

		// ensure it has the right format
		if (!dict.CanLoadNative(resourceUri))
			return null;

		// load it
		dict.LoadNative(resourceUri, localizationManager);

		return dict;
	}
	private static ILocalizableResource? TryLoadWpfResourceDictionary(String filePath, LocalizationManager localizationManager)
	{
		Uri resourceUri = new(filePath, UriKind.Absolute);

		// create a localizable resource dictionary
		LocalizableResourceDictionary dict = new();

		// ensure it has the right format
		if (!dict.CanLoadNative(resourceUri))
			return null;

		// load it
		dict.LoadNative(resourceUri, localizationManager);

		return dict;
	}
	private static ILocalizableResource? TryLoadMauiResourceDictionary(String filePath, LocalizationManager localizationManager)
	{
		Uri resourceUri = new(filePath, UriKind.Absolute);

		// create a localizable resource dictionary
		LocalizableMauiResourceDictionary dict = new();

		// ensure it has the right format
		if (!dict.CanLoadNative(resourceUri))
			return null;

		// load it
		dict.LoadNative(resourceUri, localizationManager);

		return dict;
	}

	public ILocalizableResource? GetResourceTranslation(LocaleInfo locale)
	{
		LocalizationManager lm = LocalizationManager
			?? throw new InvalidOperationException("Localizable Resource File is not loaded");

#pragma warning disable IDE0018 // Inline variable declaration
		ILocalizableResource? dict;
#pragma warning restore IDE0018 // Inline variable declaration

		// try to find an already loaded string dictionary
		if (LocalizableResourceTranslations.TryGetValue(locale.Name, out dict))
			return dict;

		// avoid re-loading the native resource for locales known to have no translation
		if (_missingTranslations.Contains(locale.Name))
			return null;

		// try to load and apply the translation if not found
		if (ResourceType == LocalizableResourceType.StringDictionary)
			dict = TryLoadStringDictionary(FullPath, lm);
		else if (ResourceType == LocalizableResourceType.WpfResourceDictionary)
			dict = TryLoadWpfResourceDictionary(FullPath, lm);
		else if (ResourceType == LocalizableResourceType.MauiResourceDictionary)
			dict = TryLoadMauiResourceDictionary(FullPath, lm);
		else
			throw new InvalidOperationException("Localizable Resource File is not loaded");

		if (dict != null)
		{
			// load the translation
			// and register in the map if succeeded
			if (dict.LoadTranslation(locale))
			{
				LocalizableResourceTranslations.Add(locale.Name, dict);
			}
			else
			{
				// no translation file for this locale - remember the miss and report null
				// instead of leaking the native contents into a translation column
				_missingTranslations.Add(locale.Name);
				dict = null;
			}
		}

		return dict;
	}
	public IEnumerable<KeyValuePair<String, String>>? GetNativeContents()
	{
		return LocalizableResource?.Enumerate();
	}
	public IEnumerable<KeyValuePair<String, String>>? GetTranslations(LocaleInfo locale)
	{
		return GetResourceTranslation(locale)?.Enumerate();
	}

	public void SaveTranslations(LocaleInfo locale, IEnumerable<KeyValuePair<String, String>> translations)
	{
		ILocalizableResource? locResource = LocalizableResource;
		if (locResource == null)
			return;

		// create an empty translation file
		// this will ensure to have the below GetResourceTranslation call succeeded
		locResource.CreateTranslation(locale);
		_missingTranslations.Remove(locale.Name);

		// apply & save translations
		ILocalizableResource? locResourceTrans = GetResourceTranslation(locale);
		if (locResourceTrans != null)
		{
			locResourceTrans.UpdateTranslations(translations);
			locResourceTrans.SaveTranslation();
		}
	}
	public void CreateTranslation(LocaleInfo locale)
	{
		ILocalizableResource? locResource = LocalizableResource;
		if (locResource == null)
			return;

		// create an empty translation file
		locResource.CreateTranslation(locale);
		_missingTranslations.Remove(locale.Name);
	}
	public void DeleteTranslation(LocaleInfo locale)
	{
		ILocalizableResource? locResource = LocalizableResource;
		if (locResource == null)
			return;

		// delete the translation file
		locResource.DeleteTranslation(locale);

		// the translation file is gone - drop the cached dictionary and remember the miss
		LocalizableResourceTranslations.Remove(locale.Name);
		_missingTranslations.Add(locale.Name);
	}
}
