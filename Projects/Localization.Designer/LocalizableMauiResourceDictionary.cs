using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Xml;

namespace Armat.Localization.Designer;

internal class LocalizableMauiResourceDictionary : ILocalizableResource
{
	// XML namespaces that differ between WPF and MAUI XAML
	private const String WpfDefaultNs = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
	private const String MauiDefaultNs = "http://schemas.microsoft.com/dotnet/2021/maui";
	private const String WpfXamlNs = "http://schemas.microsoft.com/winfx/2006/xaml";
	private const String MauiXamlNs = "http://schemas.microsoft.com/winfx/2009/xaml";
	private const String WpfLibraryNs = "clr-namespace:Armat.Localization.Wpf;assembly=armat.localization.wpf";
	private const String MauiLibraryNs = "clr-namespace:Armat.Localization.Maui;assembly=armat.localization.maui";

	// Native and translation file extensions (same as WPF: .xaml / .trd)
	private const String _nativeFileExt = "xaml";
	private const String _transFileExt = "trd";
	private static readonly String[] _nativeFileExtArray = new String[] { _nativeFileExt };
	private static readonly String[] _transFileExtArray = new String[] { _transFileExt };

	public static String NativeFileExtension => _nativeFileExt;
	public static String TranslationFileExtension => _transFileExt;
	public String[] NativeFileExtensions => _nativeFileExtArray;
	public String[] TranslationFileExtensions => _transFileExtArray;

	private Wpf.LocalizableResourceDictionary _innerDictionary;

	private LocalizationManager? _locMgr;
	private Uri? _mauiSource;
	private LocaleInfo _currLocale;

	public LocalizableMauiResourceDictionary()
	{
		_innerDictionary = new Wpf.LocalizableResourceDictionary();
		_currLocale = LocaleInfo.Invalid;
	}

	public Uri Source => _mauiSource!;

	public LocaleInfo CurrentLocale => _currLocale;

	// Validates that the file at sourceUri is a MAUI LocalizableResourceDictionary
	public Boolean CanLoadNative(Uri sourceUri)
	{
		try
		{
			String filePath = GetLocalPath(sourceUri);
			FileInfo fi = new(filePath);
			if (!fi.Exists || fi.Length == 0)
				return false;

			using FileStream fs = fi.OpenRead();
			XmlReaderSettings settings = new()
			{
				ConformanceLevel = ConformanceLevel.Auto
			};
			XmlReader xr = XmlReader.Create(fs, settings);

			XmlNodeType contentNodeType = xr.MoveToContent();
			if (contentNodeType != XmlNodeType.Document && contentNodeType != XmlNodeType.Element)
				return false;

			if (!xr.LocalName.Equals("LocalizableResourceDictionary", StringComparison.InvariantCultureIgnoreCase))
				return false;

			String? defaultNs = xr.GetAttribute("xmlns");
			if (!String.Equals(defaultNs, MauiDefaultNs, StringComparison.OrdinalIgnoreCase))
				return false;
		}
		catch
		{
			return false;
		}
		return true;
	}

	public void LoadNative(Uri sourceUri, LocalizationManager localizationManager)
	{
		_locMgr = localizationManager;
		_mauiSource = sourceUri;
		_currLocale = LocaleInfo.Invalid;

		String mauiPath = GetLocalPath(sourceUri);
		String tempPath = CreateTempXamlPath();
		try
		{
			ConvertMauiToWpf(mauiPath, tempPath);
			_innerDictionary.LoadNative(new Uri(tempPath, UriKind.Absolute), localizationManager);
		}
		finally
		{
			TryDeleteFile(tempPath);
		}
	}

	public Boolean LoadTranslation(LocaleInfo locale)
	{
		LocalizationManager lm = _locMgr ?? LocalizationManager.Default;
		TranslationLoadBehavior loadBehavior = lm.Configuration.TranslationLoadBehavior;

		if (!locale.IsValid)
		{
			ResetTranslationForKeys(_innerDictionary.Keys, loadBehavior);
			return false;
		}

		_currLocale = locale;

		String mauiTransPath = GetTranslationFilePath(locale);
		if (String.IsNullOrEmpty(mauiTransPath))
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));

		FileInfo locFile = new(mauiTransPath);
		if (!locFile.Exists)
		{
			ResetTranslationForKeys(_innerDictionary.Keys, loadBehavior);
			return false;
		}

		String tempPath = CreateTempXamlPath();
		try
		{
			ConvertMauiToWpf(mauiTransPath, tempPath);

			using FileStream fs = File.OpenRead(tempPath);
			HashSet<Object> unusedKeys = _innerDictionary.Keys.OfType<Object>().ToHashSet();

			using XmlReader xr = XmlReader.Create(fs);
			if (XamlReader.Load(xr) is ResourceDictionary loaded)
			{
				foreach (Object key in loaded.Keys)
				{
					if (unusedKeys.Remove(key))
						_innerDictionary[key] = loaded[key];
				}
			}

			ResetTranslationForKeys(unusedKeys, loadBehavior);
		}
		finally
		{
			TryDeleteFile(tempPath);
		}

		return true;
	}

	public void SaveTranslation()
	{
		if (!_currLocale.IsValid)
			throw new InvalidOperationException("No locale is loaded to save the translation for");

		String mauiTransPath = GetTranslationFilePath(_currLocale);
		if (String.IsNullOrEmpty(mauiTransPath))
			throw new InvalidOperationException($"Failed to compose name of translation file for locale {_currLocale.Name}");

		FileInfo locFile = new(mauiTransPath);
		DirectoryInfo? locDir = locFile.Directory;
		if (locDir != null && !locDir.Exists)
			locDir.Create();

		String tempPath = CreateTempXamlPath();
		try
		{
			SaveInnerDictionaryToWpfFile(tempPath);
			ConvertWpfToMaui(tempPath, mauiTransPath);
		}
		finally
		{
			TryDeleteFile(tempPath);
		}
	}

	public void CreateTranslation(LocaleInfo locale)
	{
		if (!locale.IsValid)
			throw new ArgumentException("Invalid locale", nameof(locale));

		String mauiTransPath = GetTranslationFilePath(locale);
		if (mauiTransPath.Length == 0)
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));

		FileInfo locFile = new(mauiTransPath);
		if (locFile.Exists)
			return;

		DirectoryInfo locDir = locFile.Directory!;
		if (!locDir.Exists)
			locDir.Create();

		String tempPath = CreateTempXamlPath();
		try
		{
			using (FileStream fs = File.Create(tempPath))
			{
				ResourceDictionary rd = new();
				XamlWriter.Save(rd, fs);
			}
			ConvertWpfToMaui(tempPath, mauiTransPath);
		}
		finally
		{
			TryDeleteFile(tempPath);
		}
	}

	public void DeleteTranslation(LocaleInfo locale)
	{
		if (!locale.IsValid)
			throw new ArgumentException("Invalid locale", nameof(locale));

		String mauiTransPath = GetTranslationFilePath(locale);
		if (mauiTransPath.Length == 0)
			throw new ArgumentException($"Failed to compose name of translation file for locale {locale.Name}", nameof(locale));

		FileInfo locFile = new(mauiTransPath);
		if (!locFile.Exists)
			return;

		locFile.Delete();

		DirectoryInfo locDir = locFile.Directory!;
		if (!locDir.EnumerateFileSystemInfos().Any())
			locDir.Delete();
	}

	public IEnumerable<KeyValuePair<String, String>> Enumerate()
	{
		return _innerDictionary.Enumerate();
	}

	public void UpdateTranslations(IEnumerable<KeyValuePair<String, String>> translations)
	{
		if (!_currLocale.IsValid)
			throw new NotSupportedException("Cannot update native language resource dictionary");

		foreach (KeyValuePair<String, String> pair in translations)
		{
			if (_innerDictionary.Contains(pair.Key))
				_innerDictionary[pair.Key] = pair.Value;
		}
	}

	// Mirrors Wpf.LocalizableResourceDictionary.GetTranslationFilePath using the original MAUI source URI.
	// The inner dictionary's own Source points at a temporary WPF file, so its path computation cannot be reused.
	private String GetTranslationFilePath(LocaleInfo locale)
	{
		if (_locMgr == null)
			throw new InvalidOperationException("Localization manager has not been set");
		if (_mauiSource == null)
			throw new InvalidOperationException("Source has not been set");

		String xamlFileName = GetLocalPath(_mauiSource);

		String rootPath = _locMgr.Configuration.TranslationsDirectoryPath
			.Replace('/', Path.DirectorySeparatorChar);
		if (rootPath.Length > 0 && xamlFileName.StartsWith(rootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
			xamlFileName = xamlFileName[(rootPath.Length + 1)..];

		DirectoryInfo? localeDir = _locMgr.GetTranslationsDirectory(locale.Name);
		if (localeDir == null)
			throw new DirectoryNotFoundException($"Failed to retrieve the translations directory path for locale {locale.Name}");

		xamlFileName = Path.Combine(localeDir.FullName, xamlFileName);

		if (xamlFileName.EndsWith("." + _nativeFileExt, StringComparison.OrdinalIgnoreCase))
			xamlFileName = xamlFileName[..^(_nativeFileExt.Length + 1)];
		else
			throw new FileNotFoundException("LocalizableMauiResourceDictionary Source Uri is not formatted correctly", xamlFileName);

		if (_transFileExt.Length > 0)
			xamlFileName += "." + _transFileExt;

		return xamlFileName;
	}

	private void ResetTranslationForKeys(IEnumerable keys, TranslationLoadBehavior loadBehavior)
	{
		Object[] keyArray = keys.OfType<Object>().ToArray();

		if (loadBehavior == TranslationLoadBehavior.ClearNative)
		{
			foreach (Object key in keyArray)
				_innerDictionary[key] = String.Empty;
		}
		else if (loadBehavior == TranslationLoadBehavior.RemoveNative)
		{
			foreach (Object key in keyArray)
				_innerDictionary.Remove(key);
		}
	}

	// Serializes _innerDictionary contents into a WPF-format XAML file, mirroring
	// Wpf.LocalizableResourceDictionary.SaveTranslation formatting (full element tags).
	private void SaveInnerDictionaryToWpfFile(String filePath)
	{
		using FileStream fs = File.Create(filePath);

		ResourceDictionary rd = new();
		IDictionaryEnumerator enumerator = _innerDictionary.GetEnumerator();
		while (enumerator.MoveNext())
			rd.Add(enumerator.Key, enumerator.Value);

		using MemoryStream ms = new();
		XamlWriter.Save(rd, ms);
		ms.Seek(0, SeekOrigin.Begin);

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
			VerifyFullElementTags(rootElement);
		xmlDoc.Save(fw);

		static void VerifyFullElementTags(XmlElement element)
		{
			element.IsEmpty = false;
			foreach (XmlNode node in element.ChildNodes)
			{
				if (node is XmlElement childElement)
					VerifyFullElementTags(childElement);
			}
		}
	}

	private static void ConvertMauiToWpf(String srcPath, String destPath)
	{
		String content = File.ReadAllText(srcPath);
		String converted = SwitchNamespaces(content, mauiToWpf: true);
		File.WriteAllText(destPath, converted);
	}

	private static void ConvertWpfToMaui(String srcPath, String destPath)
	{
		String content = File.ReadAllText(srcPath);
		String converted = SwitchNamespaces(content, mauiToWpf: false);
		File.WriteAllText(destPath, converted);
	}

	// The WPF default namespace contains the WPF xaml namespace as a substring,
	// so the longer/more-specific URI must be replaced first to avoid corruption.
	private static String SwitchNamespaces(String content, Boolean mauiToWpf)
	{
		if (mauiToWpf)
		{
			content = content.Replace(MauiDefaultNs, WpfDefaultNs);
			content = content.Replace(MauiXamlNs, WpfXamlNs);
			content = content.Replace(MauiLibraryNs, WpfLibraryNs);
		}
		else
		{
			content = content.Replace(WpfDefaultNs, MauiDefaultNs);
			content = content.Replace(WpfXamlNs, MauiXamlNs);
			content = content.Replace(WpfLibraryNs, MauiLibraryNs);
		}
		return content;
	}

	private static String GetLocalPath(Uri uri)
	{
		if (uri.IsAbsoluteUri && uri.IsFile)
			return uri.LocalPath;
		return uri.OriginalString;
	}

	private static String CreateTempXamlPath()
	{
		return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".xaml");
	}

	private static void TryDeleteFile(String path)
	{
		try
		{
			if (File.Exists(path))
				File.Delete(path);
		}
		catch
		{
		}
	}
}
