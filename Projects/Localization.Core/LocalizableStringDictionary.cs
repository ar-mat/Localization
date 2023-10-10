using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

namespace Armat.Localization;

public class LocalizableStringDictionary : Dictionary<String, String>, ISupportInitialize, ILocalizationTarget, ILocalizableResource
{
	public LocalizableStringDictionary()
	{
		Logger = NullLogger.Instance;
		TranslationsDirRelativePath = String.Empty;

		_currLocale = LocaleInfo.Invalid;
		_loadedLocale = LocaleInfo.Invalid;
	}
	public LocalizableStringDictionary(String sourceUri)
		: this(new Uri(sourceUri, UriKind.RelativeOrAbsolute), LocalizationManager.Default)
	{
	}
	public LocalizableStringDictionary(String sourceUri, LocalizationManager locManager)
		: this(new Uri(sourceUri, UriKind.RelativeOrAbsolute), locManager)
	{
	}
	public LocalizableStringDictionary(Uri source)
		: this(source, LocalizationManager.Default)
	{
	}
	public LocalizableStringDictionary(Uri source, LocalizationManager locManager)
	{
		Logger = NullLogger.Instance;
		TranslationsDirRelativePath = String.Empty;

		_currLocale = LocaleInfo.Invalid;
		_loadedLocale = LocaleInfo.Invalid;

		_source = source;

		// register string dictionary in localization manager to receive further localization change events
		LocalizationManager = locManager;
	}

	// Implementation of System.ComponentModel.ISupportInitialize interface
	void ISupportInitialize.BeginInit()
	{
	}
	void ISupportInitialize.EndInit()
	{
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
	// nsd = native string dictionary
	private const String _nativeFileExt = "xaml";
	private static readonly String[] _nativeFileExtArray = new String[] { _nativeFileExt };
	public static String NativeFileExtension => _nativeFileExt;
	public String[] NativeFileExtensions => _nativeFileExtArray;

	// Represents list of supported extensions for localizable file translations
	// tsd = translated string dictionary
	private const String _transFileExt = "tsd";
	private static readonly String[] _transFileExtArray = new String[] { _transFileExt };
	public static String TranslationFileExtension => _transFileExt;
	public String[] TranslationFileExtensions => _transFileExtArray;

	// The source of localizable dictionary
	private Uri? _source = null;
	public Uri Source
	{
		get
		{
			return _source ?? new Uri(String.Empty);
		}
		set
		{
			_source = value;

			// try to auto-load
			if (ResourceFilePath.Length > 0)
				LoadNative();
		}
	}

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
	public void OnLocalizationChanged(LocalizationManager locManager, LocalizationChangeEventArgs args)
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
	public String GetValueOrDefault(String key, String defaultValue)
	{
		String? result = this[key];
		return result ?? defaultValue;
	}

	// helper method to form a localizable resource Uri from the appropriate code behind type
	public static Uri FormResourceUri(Type type)
	{
		String? asmName = type.Assembly.GetName().Name;
		String? typeName = type.FullName;

		if (asmName == null || typeName == null)
			return new Uri(String.Empty);

		return new Uri($"/{asmName};component/{typeName}.{NativeFileExtension}", UriKind.Relative);
	}

	// native and translation file paths
	public String ResourceFilePath
	{
		get
		{
			Uri? uri = _source;
			if (uri == null)
				return String.Empty;

			if (uri.IsAbsoluteUri)
				return uri.AbsolutePath;

			return uri.OriginalString;
		}
	}
	public String GetNativeFilePath()
	{
		Uri? uri = _source;
		if (uri == null)
			return String.Empty;

		if (!uri.IsAbsoluteUri)
			return String.Empty;

		return uri.AbsolutePath;
	}
	public Stream? GetNativeResourceStream()
	{
		Uri? uri = _source;
		if (uri == null)
			return null;

		if (uri.IsAbsoluteUri)
			return null;

		// split the URI to determine the assembly name and resource path
		String resourcePath = uri.OriginalString;
		Int32 sepIndex = resourcePath.IndexOf(';');
		Assembly? asm;

		if (sepIndex > 0)
		{
			String assemblyName = resourcePath[..sepIndex].TrimStart('/');
			resourcePath = resourcePath[(sepIndex + 1)..];
			asm = Assembly.Load(assemblyName);
			if (asm == null)
			{
				Logger.LogError("Failed to load assembly with name {assemblyName}", assemblyName);
				throw new TypeLoadException($"Assembly with name {assemblyName} is not found");
			}
		}
		else
		{
			asm = Assembly.GetEntryAssembly();
			if (asm == null)
			{
				Logger.LogError("Failed to retrieve entry assembly");
				throw new TypeLoadException("Failed to retrieve entry assembly");
			}
		}

		if (resourcePath.StartsWith("component/"))
			resourcePath = resourcePath.Remove(0, "component/".Length);

		return asm.GetManifestResourceStream(resourcePath);
	}

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

		// by default, it will try to locate the translation directory path based on assumption that assembly name matches the default namespace name of assembly
		if (TranslationsDirRelativePath.Length == 0)
		{
			// ensure to have only the xaml file path in xamlFileName
			Int32 pathSepIndex = xamlFileName.LastIndexOf(';');
			String assemblyName = String.Empty;

			if (pathSepIndex != -1)
			{
				assemblyName = xamlFileName[..pathSepIndex].TrimStart('/');
				xamlFileName = xamlFileName[(pathSepIndex + 1)..];
			}
			if (xamlFileName.StartsWith("component/"))
			{
				xamlFileName = xamlFileName.Remove(0, "component/".Length);

				// also try to remove the assembly name - considering it coincides with the default namespace
				if (assemblyName.Length > 0 && xamlFileName.StartsWith(assemblyName + '.', StringComparison.OrdinalIgnoreCase))
					xamlFileName = xamlFileName.Remove(0, assemblyName.Length + 1);

				// replace all '.'-s with '/'-s without the last one (excluding the file extension)
				Char[] arrFileNameChars = xamlFileName.ToCharArray();
				Int32 dirSepIndex = Array.LastIndexOf(arrFileNameChars, '.');
				Boolean isLast = true;
				while (dirSepIndex != -1)
				{
					if (!isLast)
						arrFileNameChars[dirSepIndex] = Path.DirectorySeparatorChar;
					else
						isLast = false;

					dirSepIndex = Array.LastIndexOf(arrFileNameChars, '.', dirSepIndex - 1);
				}
				xamlFileName = new String(arrFileNameChars);
			}
		}
		else
		{
			// use translations directory path is specified explicitly

			// get the localizable file name without directory information
			String localizableFileName;
			Int32 dirSepIndex = xamlFileName.LastIndexOf('.');

			if (dirSepIndex != -1)
			{
				dirSepIndex = xamlFileName.LastIndexOf('.', dirSepIndex - 1);
				if (dirSepIndex == -1)
					localizableFileName = xamlFileName;
				else
					localizableFileName = xamlFileName[(dirSepIndex + 1)..];
			}
			else
			{
				localizableFileName = xamlFileName;
			}

			// prepend the path
			xamlFileName = Path.Combine(TranslationsDirRelativePath, localizableFileName);
		}

		// normalize directory separator chars
		xamlFileName = xamlFileName.Replace('/', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);

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

	// loading the native (non-translated) file
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
			XmlReader xr = XmlReader.Create(fs);

			// move to the document element
			XmlNodeType contentNodeType = xr.MoveToContent();
			if (contentNodeType != XmlNodeType.Document && contentNodeType != XmlNodeType.Element)
				return false;

			// verify document element name
			if (!xr.LocalName.Equals(LocalizationDocument.RootXmlElementName, StringComparison.InvariantCultureIgnoreCase))
				return false;
		}
		catch (Exception ex)
		{
			Logger.LogWarning(ex, "Failed to determine if it can load from source URI {sourceUri}", sourceUri);
			return false;
		}

		return true;
	}
	private Boolean _isLoaded = false;
	public void LoadNative(Uri sourceUri, LocalizationManager localizationManager)
	{
		// use the provided localization manager
		LocalizationManager = localizationManager;

		// try to load the file
		((ISupportInitialize)this).BeginInit();
		Source = sourceUri;
		((ISupportInitialize)this).EndInit();
	}
	public void LoadNative()
	{
		// reset current data in case it's reloading
		Clear();

		// try to load the native localization file
		Stream? embeddedResourceStream;
		String? xamlFileName = GetNativeFilePath();

		if (!String.IsNullOrEmpty(xamlFileName))
		{
			FileInfo nativeFile = new(xamlFileName);
			if (nativeFile.Exists)
			{
				// try to load the file
				LoadNative(nativeFile);
			}
			else
			{
				Logger.LogError("Localizable native file {xamlFileName} is not found", xamlFileName);
				throw new FileNotFoundException("LocalizableStringDictionary Native file is not found", xamlFileName);
			}
		}
		else if ((embeddedResourceStream = GetNativeResourceStream()) != null)
		{
			// try to load resource stream
			LoadNative(embeddedResourceStream);
		}
		else
		{
			Logger.LogError("Localizable native file {xamlFileName} is not found", xamlFileName);
			throw new InvalidOperationException("Source is not initialized");
		}
	}
	private void LoadNative(FileInfo nativeFileInfo)
	{
		try
		{
			// open file stream
			using FileStream fs = nativeFileInfo.OpenRead();

			// load the string dictionary
			LoadNative(fs);
		}
		catch (Exception ex)
		{
			// log and rethrow
			Logger.LogError(ex, "Localizable native file {NativeFileName} loading failed", nativeFileInfo.FullName);
			throw;
		}
	}
	private void LoadNative(Stream stream)
	{
		// set the loaded flag
		_isLoaded = true;

		// reset the loaded locale if the native file is being loaded
		_loadedLocale = LocaleInfo.Invalid;

		// reset current data in case it's reloading
		Clear();

		// load the string dictionary
		LocalizationDocument? docDeserialized = LocalizationDocument.Load(stream);
		if (docDeserialized != null && docDeserialized.Records != null)
		{
			foreach (TextRecord record in docDeserialized.Records)
			{
				if (!String.IsNullOrEmpty(record.Key) && record.Value != null)
				{
					if (!ContainsKey(record.Key))
						Add(record.Key, record.Value);
					else
						Logger.LogWarning("Localizable native stream contains duplicate key {Key}", record.Key);
				}
			}
		}
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
		LoadTranslation(locFileInfo, loadBehavior);

		// save the loaded locale info
		_loadedLocale = _currLocale;

		return true;
	}
	private void LoadTranslation(FileInfo locFileInfo, TranslationLoadBehavior loadBehavior)
	{
		// ensure to have the native dictionary loaded before localizing it
		if (!_isLoaded)
			throw new InvalidOperationException("Source is not initialized");

		try
		{
			// open file stream
			using FileStream fs = locFileInfo.OpenRead();

			// set of unused keys
			HashSet<String> unusedKeys = Keys.OfType<String>().ToHashSet();

			// load the string dictionary
			LocalizationDocument? docDeserialized = LocalizationDocument.Load(fs);
			if (docDeserialized != null && docDeserialized.Records != null)
			{
				foreach (TextRecord record in docDeserialized.Records)
				{
					// ensure to replace only existing keys, do not add new ones
					if (!String.IsNullOrEmpty(record.Key) && unusedKeys.Remove(record.Key))
						this[record.Key] = record.Value;
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
	private void ResetTranslationForKays(IEnumerable<String> keys, TranslationLoadBehavior loadBehavior)
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

			// create the document to be saved into a file
			LocalizationDocument docForSerialize = new();

			Int32 index = 0;
			docForSerialize.Records = new TextRecord[Count];
			foreach (KeyValuePair<String, String> item in this)
			{
				docForSerialize.Records[index].Key = (String)item.Key;
				docForSerialize.Records[index].Value = (String)(item.Value ?? String.Empty);
				index++;
			}

			// save it
			docForSerialize.Save(fs);
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Translation file {LocalizationFileName} saving failed", locFileInfo.FullName);
			throw;
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

		// create the document to be saved into a file
		LocalizationDocument docForSerialize = new()
		{
			Records = Array.Empty<TextRecord>()
		};

		// save it
		docForSerialize.Save(fs);
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
				Where(de => de.Value != null).
				OrderBy(pair => pair.Key, StringComparer.InvariantCultureIgnoreCase);
	}
	public void UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)
	{
		if (!_currLocale.IsValid)
			throw new NotSupportedException("Cannot update native language string dictionary");

		foreach (KeyValuePair<String, String> pair in translations)
		{
			if (ContainsKey(pair.Key))
				this[pair.Key] = pair.Value;
		}
	}
}
