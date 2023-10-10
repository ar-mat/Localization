using System;

using StringDictionary = Armat.Localization.Demo.Lib.Localization.StringDictionary;

namespace Armat.Localization.Demo.Lib;

public static class ClassFromLibrary
{
	public static String ErrorCaption => StringDictionary.Instance.MessageBox_Caption_Error;
	public static String WarningCaption => StringDictionary.Instance.MessageBox_Caption_Warning;
	public static String InfoCaption => StringDictionary.Instance.MessageBox_Caption_Info;

	public static String UnknownFailureMessage =>
		StringDictionary.Instance.LocalizedStrings.GetValueOrDefault("ErrorMessage_SomethingFailed", ":(");

	public static String GetManuallyTranslatedString()
	{
		LocalizableStringDictionary dict = StringDictionary.Instance.LocalizedStrings;
		return dict.GetValueOrDefault("TextBox_ManuallyTranslatedValue", "Failed!");
	}

	public static String ComposeInfoMessage(String textBoxValue)
	{
		LocalizableStringDictionary dict = StringDictionary.Instance.LocalizedStrings;
		String? messageFormat;

		// get the message format
		if (!dict.TryGetValue("InfoMessage_Parametrized_Lang_Text", out messageFormat) ||
			String.IsNullOrEmpty(messageFormat))
		{
			return String.Empty;
		}

		// compose the message
		return String.Format(messageFormat, dict.LocalizationManager.CurrentLocale, textBoxValue);
	}
}
