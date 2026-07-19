using Armat.Localization;

using Microsoft.Maui.Controls;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace MauiApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
	}

	public List<LocaleInfo> AllLocales =>
		LocalizationManager.Default.AllLocales.ToList();

	public LocaleInfo CurrentLocale
	{
		get => LocalizationManager.Default.CurrentLocale;
		set
		{
			if (value != LocalizationManager.Default.CurrentLocale)
			{
				// the [Native] locale has no culture - fall back to the installed UI culture
				CultureInfo culture = value.Culture ?? CultureInfo.InstalledUICulture;
				Thread.CurrentThread.CurrentCulture = culture;
				Thread.CurrentThread.CurrentUICulture = culture;

				LocalizationManager.Default.ChangeLocale(value);
			}
		}
	}
}
