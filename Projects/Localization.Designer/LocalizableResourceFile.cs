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

	public String FileName
	{
		get => System.IO.Path.GetFileName(FullPath) ?? String.Empty;
	}
	public String DirectoryPath
	{
		get => System.IO.Path.GetDirectoryName(FullPath) ?? String.Empty;
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
			_ => throw new InvalidOperationException()
		};
	}

	public ILocalizableResource? LocalizableResource { get; private set; }
	private Dictionary<String, ILocalizableResource> LocalizableResourceTranslations { get; set; }

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
		LocalizableResource = null;
		ResourceType = LocalizableResourceType.Unknown;
		LocalizableResourceTranslations.Clear();
	}
	public Boolean Load(String resourceFilePath)
	{
		// reset loaded contents
		Reset();

		// Set file path property
		FullPath = resourceFilePath;

		// create an isolated localization manager for this file
		// it will be used to load / save localizations for the current resource
		Configuration cfg = new()
		{
			// use full file path, so it would be possible to locate the appropriate localization files
			TranslationsDirectoryPath = DirectoryPath,
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

	public ILocalizableResource? GetResourceTranslation(LocaleInfo locale)
	{
#pragma warning disable IDE0270 // Use coalesce expression
		LocalizationManager? lm = LocalizationManager;
		if (lm == null)
			throw new InvalidOperationException("Localizable Resource File is not loaded");
#pragma warning restore IDE0270 // Use coalesce expression

#pragma warning disable IDE0018 // Inline variable declaration
		ILocalizableResource? dict;
#pragma warning restore IDE0018 // Inline variable declaration

		// try to find an already loaded string dictionary
		if (LocalizableResourceTranslations.TryGetValue(locale.Name, out dict))
			return dict;

		// try to load and apply the translation if not found
		if (ResourceType == LocalizableResourceType.StringDictionary)
			dict = TryLoadStringDictionary(FullPath, lm);
		else if (ResourceType == LocalizableResourceType.WpfResourceDictionary)
			dict = TryLoadWpfResourceDictionary(FullPath, lm);
		else
			throw new InvalidOperationException("Localizable Resource File is not loaded");

		if (dict != null)
		{
			// load the translation
			// and register in the map if succeeded
			if (dict.LoadTranslation(locale))
				LocalizableResourceTranslations.Add(locale.Name, dict);
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
	}
	public void DeleteTranslation(LocaleInfo locale)
	{
		ILocalizableResource? locResource = LocalizableResource;
		if (locResource == null)
			return;

		// delete the translation file
		locResource.DeleteTranslation(locale);
	}
}