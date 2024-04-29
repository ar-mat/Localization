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

		// get the message format
		if (dict.TryGetValue("InfoMessage_Parametrized_Lang_Text", out String? messageFormat) &&
			!String.IsNullOrEmpty(messageFormat))
		{
			// compose the message
			return String.Format(messageFormat, dict.LocalizationManager.CurrentLocale, textBoxValue);
		}

		return String.Empty;
	}
}
