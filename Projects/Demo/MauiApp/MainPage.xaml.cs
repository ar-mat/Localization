using Armat.Localization;

using Microsoft.Maui.Accessibility;
using Microsoft.Maui.Controls;

using System;
using System.Collections.Generic;

namespace MauiApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();

		LocalizationManager.Default.LocalizationChanged += OnLocaleChanged;
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		CounterBtn.Text = GetClickMeCounterText(_rdStringTable, count);

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

	private void OnLocaleChanged(Object sender, LocalizationChangeEventArgs e)
	{
		CounterBtn.Text = GetClickMeCounterText(_rdStringTable, count);

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

	public String GetClickMeCounterText(ResourceDictionary rd, Int32 count)
	{
		String counterFormat = rd["Btn_ClickMe_Counter"] as String
			?? throw new KeyNotFoundException("Btn_ClickMe_Counter");

		return String.Format(counterFormat, count);
	}
}
