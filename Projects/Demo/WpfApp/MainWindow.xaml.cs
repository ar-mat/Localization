using Armat.Localization.Demo.Lib;

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Armat.Localization.Demo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
	public MainWindow()
	{
		InitializeComponent();

		LocalizationManager.Default.LocalizationChanged += OnLocaleChanged;
		_tbManuallyTranslated.Text = ClassFromLibrary.GetManuallyTranslatedString();
	}

	private void OnLocaleChanged(Object sender, LocalizationChangeEventArgs e)
	{
		// update the text on locale change
		_tbManuallyTranslated.Text = ClassFromLibrary.GetManuallyTranslatedString();

		NotifyPropertyChanged(nameof(NestedText1));
		NotifyPropertyChanged(nameof(NestedText2));
	}

	private Popup? PopupWindow { get; set; }
	private void OnShowPopupClick(Object sender, RoutedEventArgs e)
	{
		PopupWindow ??= new();
		PopupWindow.Closing += (sender, e) => PopupWindow = null;

		PopupWindow.Show();
		PopupWindow.Activate();
	}

	private void OnComboBoxSelectionChanged(Object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0)
		{
			LocaleInfo? selectedLocale = e.AddedItems.Cast<LocaleInfo>().FirstOrDefault();
			if (selectedLocale != null && selectedLocale.Culture != null)
			{
				Thread.CurrentThread.CurrentCulture = selectedLocale.Culture;
				Thread.CurrentThread.CurrentUICulture = selectedLocale.Culture;

				LocalizationManager.Default.ChangeLocale(selectedLocale);
			}
		}
	}

	private void OnWarningMessageButtonClick(Object sender, RoutedEventArgs e)
	{
		MessageBox.Show(_tbAutoTranslated.Text, ClassFromLibrary.WarningCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
	}

	private void OnInfoMessageButtonClick(Object sender, RoutedEventArgs e)
	{
		String infoMessageText = ClassFromLibrary.ComposeInfoMessage(_tbManuallyTranslated.Text);
		if (infoMessageText.Length > 0)
			MessageBox.Show(infoMessageText, ClassFromLibrary.InfoCaption, MessageBoxButton.OK, MessageBoxImage.Information);
		else
			MessageBox.Show("Something went wrong", ClassFromLibrary.ErrorCaption, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	private void OnClose(Object sender, RoutedEventArgs e)
	{
		Close();
	}

	private void OnWindowClosing(Object sender, CancelEventArgs e)
	{
		PopupWindow?.Close();
	}

	public String NestedText1 => ClassFromLibrary.MoreString1;
	public String NestedText2 => ClassFromLibrary.MoreString2;

	public event PropertyChangedEventHandler? PropertyChanged;
	private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
