using Armat.Localization;

using Microsoft.Maui.Controls;

using System.Collections.Generic;
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
			if (value != LocalizationManager.Default.CurrentLocale && value.Culture != null)
			{
				Thread.CurrentThread.CurrentCulture = value.Culture;
				Thread.CurrentThread.CurrentUICulture = value.Culture;

				LocalizationManager.Default.ChangeLocale(value);
			}
		}
	}
}
