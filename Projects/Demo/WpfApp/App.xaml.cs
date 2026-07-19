using System;
using System.Windows;

namespace Armat.Localization.Demo;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private void OnStartup(Object sender, StartupEventArgs e)
	{
		// start in the native (untranslated) locale - [Native] is also offered in the language
		// selector because LocalizationManager.AllLocales prepends the default locale when it's Invalid
		Configuration config = Configuration.Default with { DefaultLocale = LocaleInfo.Invalid };

		LocalizationManager.CreateDefaultInstance(config);
	}
}
