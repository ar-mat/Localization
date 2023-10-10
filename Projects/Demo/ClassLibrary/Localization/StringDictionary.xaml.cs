using System;

namespace Armat.Localization.Demo.Lib.Localization;

internal class StringDictionary
{
	private StringDictionary() :
		this(LocalizableStringDictionary.FormResourceUri(typeof(StringDictionary)))
	{
	}
	private StringDictionary(Uri sourceUri)
	{
		LocalizedStrings = new();
		LocalizedStrings.LoadNative(sourceUri, LocalizationManager.Default);
	}

	private static StringDictionary? _instance = null;
	public static StringDictionary Instance
	{
		get
		{
			_instance ??= CreateDefaultInstance();
			return _instance;
		}
	}
	public static StringDictionary CreateDefaultInstance()
	{
		return _instance ??= new StringDictionary();
	}

	public LocalizableStringDictionary LocalizedStrings { get; init; }

	public String MessageBox_Caption_Info => LocalizedStrings["MessageBox_Caption_Info"];
	public String MessageBox_Caption_Warning => LocalizedStrings["MessageBox_Caption_Warning"];
	public String MessageBox_Caption_Error => LocalizedStrings["MessageBox_Caption_Error"];
}
