using Armat.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace MauiApp;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// start in the native (untranslated) locale by default, and offer [Native] alongside
		// the translated locales in the language selector
		Configuration config = Configuration.Default with { DefaultLocale = LocaleInfo.Invalid };
		config.SupportedLocales = new[] { LocaleInfo.Invalid, new LocaleInfo("en"), new LocaleInfo("hy"), new LocaleInfo("ru") };

		LocalizationManager.CreateDefaultInstance(config);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}