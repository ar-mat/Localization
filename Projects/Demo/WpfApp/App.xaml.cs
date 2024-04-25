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
		Configuration config = Configuration.Default with { DefaultLocale = new LocaleInfo("en") };

		LocalizationManager.CreateDefaultInstance(config);
	}
}
