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

		Configuration config = Configuration.Default with { DefaultLocale = new LocaleInfo("en") };

		LocalizationManager.CreateDefaultInstance(config);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}