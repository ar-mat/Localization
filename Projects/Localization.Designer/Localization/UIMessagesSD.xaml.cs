using System;

namespace Armat.Localization.Designer.Localization;
internal class UIMessagesSD
{
	private UIMessagesSD() :
		// "Armat.Localization.Designer.Localization.UIMessagesSD.xaml"
		//this(new Uri("/armat.localization.designer;component/Armat.Localization.Designer.Localization.UIMessagesSD.xaml", UriKind.Relative))
		this(LocalizableStringDictionary.FormResourceUri(typeof(UIMessagesSD)))
	{
	}
	private UIMessagesSD(Uri sourceUri)
	{
		LocalizedStrings = new();
		LocalizedStrings.LoadNative(sourceUri, LocalizationManager.Default);
	}

	// Lazy<T> makes the singleton initialization thread-safe
	private static readonly Lazy<UIMessagesSD> _instance = new(() => new UIMessagesSD());
	public static UIMessagesSD Instance => _instance.Value;
	public static UIMessagesSD CreateDefaultInstance()
	{
		return Instance;
	}

	public LocalizableStringDictionary LocalizedStrings { get; init; }

	public String MessageBox_Caption_Info => LocalizedStrings["MessageBox_Caption_Info"];
	public String MessageBox_Caption_Warning => LocalizedStrings["MessageBox_Caption_Warning"];
	public String MessageBox_Caption_Error => LocalizedStrings["MessageBox_Caption_Error"];

	public String UIMessage_SelectLanguage => LocalizedStrings["UIMessage_SelectLanguage"];
	public String UIMessage_DeleteTransFor_Lang => LocalizedStrings["UIMessage_DeleteTransFor_Lang"];

	public String LanguageSelector_NoSelection => LocalizedStrings["LanguageSelector_NoSelection"];
	public String OpenFileSelector_FilterLabel => LocalizedStrings["OpenFileSelector_FilterLabel"];
}
