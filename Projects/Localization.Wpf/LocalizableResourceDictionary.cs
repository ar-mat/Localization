using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace Armat.Localization.Wpf;

public class LocalizableResourceDictionary : ResourceDictionary, ISupportInitialize, ILocalizationTarget, ILocalizableResource
{
	public LocalizableResourceDictionary()
	{
		Logger = NullLogger.Instance;
		TranslationsDirRelativePath = String.Empty;

		_currLocale = LocaleInfo.Invalid;
		_loadedLocale = LocaleInfo.Invalid;
	}
	public LocalizableResourceDictionary(String sourceUri)
		: this(new Uri(sourceUri, UriKind.RelativeOrAbsolute), LocalizationManager.Default)
	{
	}
	public LocalizableResourceDictionary(String sourceUri, LocalizationManager locManager)
		: this(new Uri(sourceUri, UriKind.RelativeOrAbsolute), locManager)
	{
	}
	public LocalizableResourceDictionary(Uri source)
		: this(source, LocalizationManager.Default)
	{
	}
	public LocalizableResourceDictionary(Uri source, LocalizationManager locManager)
	{
		Logger = NullLogger.Instance;
		TranslationsDirRelativePath = String.Empty;

		_currLocale = LocaleInfo.Invalid;
		_loadedLocale = LocaleInfo.Invalid;

		Source = source;

		// register string dictionary in localization manager to receive further localization change events
		LocalizationManager = locManager;
	}

	// Implementation of System.ComponentModel.ISupportInitialize interface
	void ISupportInitialize.BeginInit()
	{
		this.BeginInit();
	}
	void ISupportInitialize.EndInit()
	{
		// ensure to have native locale contents loaded
		this.EndInit();

		// in case there's a non-native locale selected in Localization Manager, OnLocalizationChanged will be triggered
		// upon _locMgr.Targets.Add() and LoadLocalized will be called with the appropriate Locale
		if (_locMgr == null)
			LocalizationManager = LocalizationManager.Default;

		// load the translation if not loaded yet
		if (_currLocale.IsValid && _currLocale != _loadedLocale)
		{
			try
			{
				LoadTranslation(_currLocale);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, "Failed to load translation for locale {locale}", _currLocale.Name);
			}
		}
	}

	// the logger to be used for this class
	protected ILogger Logger { get; private set; }

	// path to the translations directory relative to the runtime directory
	// Use this property in case the translations directory is not being determined automatically
	public String TranslationsDirRelativePath
	{
		get; set;
	}

	// Represents list of supported extensions for localizable files in native language
	// xaml = (nsd = native resource dictionary)
	private const String _nativeFileExt = "xaml";
	private static readonly String[] _nativeFileExtArray = new String[] { _nativeFileExt };
	public static String NativeFileExtension => _nativeFileExt;
	public String[] NativeFileExtensions => _nativeFileExtArray;

	// Represents list of supported extensions for localizable file translations
	// tsd = translated resource dictionary
	private const String _transFileExt = "trd";
	private static readonly String[] _transFileExtArray = new String[] { _transFileExt };
	public static String TranslationFileExtension => _transFileExt;
	public String[] TranslationFileExtensions => _transFileExtArray;

	// Localization manager
	private LocalizationManager? _locMgr = null;
	public LocalizationManager LocalizationManager
	{
		get
		{
			return _locMgr ?? LocalizationManager.Default;
		}
		set
		{
			// check if it's trying to set the same
			if (_locMgr == value)
				return;

			// do not let it being reset
			if (_locMgr != null)
				throw new InvalidOperationException("Cannot reset the localization manager");

			// value can't be null because of above condition checks
			// _locMgr != value && _locMgr == null
			_locMgr = value!;

			// register in the localization manager
			_locMgr.Targets.Add(this);

			// create the logger if not created yet
			if (Logger == NullLogger.Instance)
				Logger = _locMgr.LoggerFactory.CreateLogger<LocalizableStringDictionary>();
		}
	}

	// Implementation of ILocalizationTarget interface
	private LocaleInfo _currLocale;
	private LocaleInfo _loadedLocale;
	public LocaleInfo CurrentLocale => _currLocale;
	void ILocalizationTarget.OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)
	{
		if (_currLocale != args.NewLocale && locManager == LocalizationManager)
		{
			// load the new locale
			if (args.NewLocale.IsValid)
			{
				LoadTranslation(args.NewLocale);
			}
			else
			{
				_currLocale = LocaleInfo.Invalid;
				LoadNative();
			}

			// check if localization has changed after LoadLocalized
			if (_currLocale != args.NewLocale)
				Logger.LogWarning("Localization change didn't happen");
		}
	}

	// String dictionary additions
	public T GetValueOrDefault<T>(String key, T defaultValue)
	{
		T result;

		try
		{
			result = (T)this[key];
			result ??= defaultValue;
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Failed to retrieve value for key {key}", key);
			result = defaultValue;
		}

		return result;
	}

	// native and translation file paths
	public String ResourceFilePath
	{
		get
		{
			Uri? uri = Source ?? ((IUriContext)this).BaseUri;
			if (uri == null)
				return String.Empty;

			if (uri.IsAbsoluteUri)
			{
				if (uri.IsFile)
					return uri.OriginalString;

				return uri.AbsolutePath;
			}

			return uri.OriginalString;
		}
	}
	//public String GetNativeFilePath()
	//{
	//    String xamlFileName = ResourceFilePath;
	//    if (xamlFileName == null)
	//        return String.Empty;

	//    return GetNativeFilePath(xamlFileName);
	//}
	//private String GetNativeFilePath(String xamlFileName)
	//{
	//    LocalizationManager lm = LocalizationManager;

	//    // normalize directory separator chars
	//    xamlFileName = xamlFileName.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

	//    // remove the starting root path - make it relative to the locMgr.Configuration.RuntimeDirectoryPath
	//    String rootPath = lm.Configuration.RuntimeDirectoryPath.Replace('/', Path.DirectorySeparatorChar);
	//    if (rootPath.Length > 0 && xamlFileName.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
	//        xamlFileName = xamlFileName[(rootPath.Length + 1)..];

	//    return xamlFileName;
	//}
	public String GetTranslationFilePath(LocaleInfo locale)
	{
		String xamlFileName = ResourceFilePath;
		if (String.IsNullOrEmpty(xamlFileName))
			return String.Empty;

		return GetTranslationFilePath(xamlFileName, locale);
	}
	private String GetTranslationFilePath(String xamlFileName, LocaleInfo locale)
	{
		LocalizationManager lm = LocalizationManager;

		if (TranslationsDirRelativePath.Length == 0)
		{
			// ensure to have only the xaml file path in xamlFileName
			Int32 pathSepIndex = xamlFileName.LastIndexOf(';');
			if (pathSepIndex != -1)
				xamlFileName = xamlFileName[(pathSepIndex + 1)..];
			if (xamlFileName.StartsWith("component/"))
				xamlFileName = xamlFileName.Remove(0, "component/".Length);

			// normalize directory separator chars
			xamlFileName = xamlFileName.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
		}
		else
		{
			// normalize directory separator chars
			xamlFileName = xamlFileName.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

			// get the localizable file name without directory information
			Int32 dirSepIndex = xamlFileName.LastIndexOf(Path.DirectorySeparatorChar);
			if (dirSepIndex != -1)
				xamlFileName = xamlFileName[(dirSepIndex + 1)..];

			// prepend the path
			xamlFileName = Path.Combine(TranslationsDirRelativePath, xamlFileName);
		}

		// remove the starting root path - make it relative to the locMgr.Configuration.RuntimeDirectoryPath
		String rootPath = lm.Configuration.TranslationsDirectoryPath.Replace('/', Path.DirectorySeparatorChar);
		if (rootPath.Length > 0 && xamlFileName.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			xamlFileName = xamlFileName[(rootPath.Length + 1)..];

		// prepend the localization directory path from localization manager
		DirectoryInfo? localeDir = lm.GetTranslationsDirectory(locale.Name);
		if (localeDir != null)
		{
			xamlFileName = Path.Combine(localeDir.FullName, xamlFileName);
		}
		else
		{
			Logger.LogError("Failed to retrieve the translations directory path for locale {locale}", locale.Name);
			throw new DirectoryNotFoundException($"Failed to retrieve the translations directory path for locale {locale.Name}");
		}

		// remove localizable file extension
		if (xamlFileName.EndsWith("." + NativeFileExtension, StringComparison.OrdinalIgnoreCase))
		{
			xamlFileName = xamlFileName[..^(NativeFileExtension.Length + 1)];
		}
		else
		{
			Logger.LogError("LocalizableResourceDictionary Source Uri {xamlFileName} is not formatted correctly", xamlFileName);
			throw new FileNotFoundException("LocalizableResourceDictionary Source Uri is not formatted correctly", xamlFileName);
		}

		// append translation file extension
		if (TranslationFileExtension.Length > 0)
			xamlFileName += "." + TranslationFileExtension;

		return xamlFileName;
	}

	// loading native language contents
	public Boolean CanLoadNative(Uri sourceUri)
	{
		try
		{
			// check file existence
			String xamlFileName = sourceUri.OriginalString;
			FileInfo fi = new(xamlFileName);
			if (!fi.Exists || fi.Length == 0)
				return false;

			// create XML reader
			using FileStream fs = fi.OpenRead();
			XmlReaderSettings readerSettings = new()
			{
				ConformanceLevel = ConformanceLevel.Auto
			};
			XmlReader xr = XmlReader.Create(fs, readerSettings);

			// move to the document element
			XmlNodeType contentNodeType = xr.MoveToContent();
			if (contentNodeType != XmlNodeType.Document && contentNodeType != XmlNodeType.Element)
				return false;

			// verify document element name
			if (!xr.LocalName.Equals("LocalizableResourceDictionary", StringComparison.InvariantCultureIgnoreCase))
				return false;
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Failed to determine if it can load from source URI {sourceUri}", sourceUri);
			return false;
		}

		return true;
	}
	public void LoadNative()
	{
		Uri uri = Source;
		if (uri != null)
			Source = new Uri(uri.OriginalString, uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative);

		// reset the loaded locale if the native file is being loaded
		_loadedLocale = LocaleInfo.Invalid;
	}
	public void LoadNative(Uri sourceUri, LocalizationManager localizationManager)
	{
		// use the provided localization manager
		LocalizationManager = localizationManager;

		// try to load the file
		((ISupportInitialize)this).BeginInit();
		Source = sourceUri;
		((ISupportInitialize)this).EndInit();
	}

	// loading translations
	public Boolean LoadTranslation(String localeName)
	{
		return LoadTranslation(new LocaleInfo(localeName));
	}
	public Boolean LoadTranslation(LocaleInfo locale)
	{
		TranslationLoadBehavior loadBehavior = LocalizationManager.Configuration.TranslationLoadBehavior;

		if (!locale.IsValid)
		{
			Logger.LogWarning("Cannot load translation from invalid locale {locale}", locale.Name);
			//throw new ArgumentException($"Cannot load translation from invalid locale {locale.Name}", nameof(locale));

			ResetTranslationForKays(Keys, loadBehavior);
			return false;
		}

		// update the locale info field
		_currLocale = locale;

		String xamlFileName = GetTranslationFilePath(locale);
		if (String.IsNullOrEmpty(xamlFileName))
		{
			Logger.LogWarning("Failed to compose name of translation file for locale {locale}", locale.Name);
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));
		}

		// get the localization file
		FileInfo locFileInfo = new(xamlFileName);
		if (!locFileInfo.Exists)
		{
			Logger.LogWarning("Translation file {xamlFileName} is not found", xamlFileName);
			//throw new ArgumentException($"Translation file {xamlFileName} is not found for locale {locale.Name}", nameof(locale));

			ResetTranslationForKays(Keys, loadBehavior);
			return false;
		}

		// load from translation file
		LoadTranslation(locFileInfo, LocalizationManager.Configuration.TranslationLoadBehavior);

		// save the loaded locale info
		_loadedLocale = _currLocale;

		return true;
	}
	private void LoadTranslation(FileInfo locFileInfo, TranslationLoadBehavior loadBehavior)
	{
		try
		{
			// open file stream
			using FileStream fs = locFileInfo.OpenRead();

			// set of unused keys
			HashSet<Object> unusedKeys = Keys.OfType<Object>().ToHashSet();

			// load localized resource dictionary
			using XmlReader fr = XmlReader.Create(fs);
			if (XamlReader.Load(fr) is ResourceDictionary dicLocalized)
			{
				// iterate by resources / update
				foreach (Object key in dicLocalized.Keys)
				{
					// ensure to replace only existing keys, do not add new ones
					if (unusedKeys.Remove(key))
						this[key] = dicLocalized[key];
				}
			}

			// process skipped keys as described by loadBehavior
			ResetTranslationForKays(unusedKeys, loadBehavior);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Translation file {LocalizationFileName} loading failed", locFileInfo.FullName);
			throw;
		}
	}
	private void ResetTranslationForKays(IEnumerable keys, TranslationLoadBehavior loadBehavior)
	{
		if (loadBehavior == TranslationLoadBehavior.ClearNative)
		{
			foreach (String key in keys)
				this[key] = String.Empty;
		}
		else if (loadBehavior == TranslationLoadBehavior.RemoveNative)
		{
			foreach (String key in keys)
				Remove(key);
		}
	}

	// saving / creating / deleting translations
	public void SaveTranslation()
	{
		// ensure the current locale is set
		// this will save current localization dictionary into the right file
		if (CurrentLocale == null || !CurrentLocale.IsValid)
		{
			Logger.LogWarning("No locale is loaded to save the translation for");
			throw new InvalidOperationException("No locale is loaded to save the translation for");
		}

		String xamlFileName = GetTranslationFilePath(CurrentLocale);
		if (String.IsNullOrEmpty(xamlFileName))
		{
			Logger.LogWarning("Failed to compose name of translation file for locale {locale}", CurrentLocale.Name);
			throw new InvalidOperationException($"Failed to compose name of translation file for locale {CurrentLocale.Name}");
		}

		// get the localization file
		FileInfo? locFileInfo = new(xamlFileName);

		// save to file
		SaveTranslation(locFileInfo);
	}
	private void SaveTranslation(FileInfo locFileInfo)
	{
		try
		{
			// open file stream
			using FileStream fs = locFileInfo.Create();

			// create the resource dictionary to be saved into a file
			ResourceDictionary rd = new();
			IDictionaryEnumerator enumerator = GetEnumerator();
			while (enumerator.MoveNext())
				rd.Add(enumerator.Key, enumerator.Value);

			// write in-memory xml
			MemoryStream ms = new();
			XamlWriter.Save(rd, ms);
			ms.Seek(0, SeekOrigin.Begin);

			// write a well-formatted xml document to disk
			XmlDocument xmlDoc = new();
			XmlWriterSettings writerSettings = new()
			{
				Indent = true,
				IndentChars = "    ",
				OmitXmlDeclaration = true
			};
			using XmlWriter fw = XmlWriter.Create(fs, writerSettings);
			xmlDoc.Load(ms);
			XmlElement? rootElement = xmlDoc.DocumentElement;
			if (rootElement != null)
				EnsureToHaveWriteFullEndElementForClosingTags(rootElement);
			xmlDoc.Save(fw);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Translation file {LocalizationFileName} saving failed", locFileInfo.FullName);
			throw;
		}

		static void EnsureToHaveWriteFullEndElementForClosingTags(XmlElement element)
		{
			// setting IsEmpty = false forces to write full XML end elements instead of teh short "/>" ending
			// this is required for System.Windows.Markup.XamlReader.Load to succeeded
			element.IsEmpty = false;

			// iterate by child element
			foreach (XmlNode node in element.ChildNodes)
			{
				if (node is XmlElement childElement)
					EnsureToHaveWriteFullEndElementForClosingTags(childElement);
			}
		}
	}
	public void CreateTranslation(LocaleInfo locale)
	{
		if (!locale.IsValid)
			throw new ArgumentException("Invalid locale", nameof(locale));

		String xamlFileName = GetTranslationFilePath(locale);
		if (xamlFileName.Length == 0)
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));

		// get the localization file
		// and do nothing if the file already exists
		FileInfo? locFileInfo = new(xamlFileName);
		if (locFileInfo.Exists)
			return;

		// ensure to have the parent directory created to place the translation files in
		DirectoryInfo locDirInfo = locFileInfo.Directory!;
		if (!locDirInfo.Exists)
			locDirInfo.Create();

		// open file stream
		using FileStream fs = locFileInfo.Create();

		// create an empty resource dictionary to be saved into a file
		ResourceDictionary rd = new();

		// save it
		XamlWriter.Save(rd, fs);
	}
	public void DeleteTranslation(LocaleInfo locale)
	{
		if (!locale.IsValid)
			throw new ArgumentException("Invalid locale", nameof(locale));

		String xamlFileName = GetTranslationFilePath(locale);
		if (xamlFileName.Length == 0)
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));

		// get the localization file
		// and do nothing if the file doesn't exist
		FileInfo? locFileInfo = new(xamlFileName);
		if (!locFileInfo.Exists)
			return;

		// delete the localization file
		locFileInfo.Delete();

		// remove the localization directory if it's empty
		DirectoryInfo locDirInfo = locFileInfo.Directory!;
		if (!locDirInfo.EnumerateFileSystemInfos().Any())
			locDirInfo.Delete();
	}

	// getting / setting translations
	public IEnumerable<KeyValuePair<String, String>> Enumerate()
	{
		return this.
				OfType<DictionaryEntry>().
				Where(de => de.Key is String && de.Value is String).
				Select(de => new KeyValuePair<String, String>((String)de.Key, (String)de.Value!)).
				OrderBy(pair => pair.Key, StringComparer.InvariantCultureIgnoreCase);
	}
	public void UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)
	{
		if (!_currLocale.IsValid)
			throw new NotSupportedException("Cannot update native language resource dictionary");

		foreach (KeyValuePair<String, String> pair in translations)
		{
			if (Contains(pair.Key))
				this[pair.Key] = pair.Value;
		}
	}
}
