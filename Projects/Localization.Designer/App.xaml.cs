using Armat.Localization.Designer.Localization;

using Microsoft.Extensions.Logging;

using System;
using System.Windows;

namespace Armat.Localization.Designer;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	private void OnAppStartup(Object sender, StartupEventArgs e)
	{
		Configuration config = Configuration.Default;
		//config.DefaultLocale = new LocaleInfo("en");

		ILoggerFactory lf = LoggerFactory.Create(builder => builder.AddConsole());

		LocalizationManager.CreateDefaultInstance(config, lf);
	}
}
