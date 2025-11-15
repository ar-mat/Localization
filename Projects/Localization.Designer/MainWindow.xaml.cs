using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

using Binding = System.Windows.Data.Binding;
using ComboBox = System.Windows.Controls.ComboBox;
using DataFormats = System.Windows.DataFormats;
using DataObject = System.Windows.DataObject;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace Armat.Localization.Designer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		LocalizableFiles = new ObservableCollection<LocalizableResourceFile>();
		TranslationsTable = new TranslationsTable();
	}

	protected override void OnClosed(EventArgs e)
	{
		base.OnClosed(e);
	}

	#region Data members

	// LocalizableFiles property
	public static readonly DependencyProperty LocalizableFilesProperty =
		DependencyProperty.Register("LocalizableFiles", typeof(ObservableCollection<LocalizableResourceFile>), typeof(MainWindow));
	public ObservableCollection<LocalizableResourceFile> LocalizableFiles
	{
		get { return (ObservableCollection<LocalizableResourceFile>)GetValue(LocalizableFilesProperty); }
		init { SetValue(LocalizableFilesProperty, value); }
	}

	// TranslationsTable property
	public static readonly DependencyProperty TranslationsTableProperty =
		DependencyProperty.Register("TranslationsTable", typeof(TranslationsTable), typeof(MainWindow), new PropertyMetadata(null, OnTranslationsTableChanged));
	public TranslationsTable TranslationsTable
	{
		get { return (TranslationsTable)GetValue(TranslationsTableProperty); }
		set { SetValue(TranslationsTableProperty, value); }
	}

	private static void OnTranslationsTableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		MainWindow window = (MainWindow)d;
		window.UpdateTranslationsDataGridColumns(false);
	}

	public static IEnumerable<LocaleInfo> SupportedLocales
	{
		get
		{
			IEnumerable<LocaleInfo> locales = LocaleInfo.AllLocales;
			LocaleInfo[] invalidLocale = new LocaleInfo[] { new LocaleInfo(CultureInfo.InvariantCulture, Localization.UIMessagesSD.Instance.LanguageSelector_NoSelection) };
			return invalidLocale.Union(locales);
		}
	}

	#endregion // Data members

	#region UI command handlers

	private void CanScanDirectory(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanAddFiles(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanRemoveSelected(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanClearFiles(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanSaveAll(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanAddLanguage(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void CanRemoveLanguage(Object sender, CanExecuteRoutedEventArgs e)
	{
		e.CanExecute = true;
	}

	private void OnScanDirectory(Object sender, ExecutedRoutedEventArgs e)
	{
		FolderBrowserDialog dlg = new();
		if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
			return;

		ScanForLocalizableResourceFiles(dlg.SelectedPath, LocalizableResourceFile.NativeFileExtensions);
	}

	private void OnAddFiles(Object sender, ExecutedRoutedEventArgs e)
	{
		String fdFilter1 = Localization.UIMessagesSD.Instance.OpenFileSelector_FilterLabel + "|" +
			LocalizableResourceFile.NativeFileExtensions.
			Select(x => "*." + x).
			Aggregate((x, y) => x + ";" + y);
		String fdFilter2 =
			LocalizableResourceFile.NativeFileExtensions.
			Select(x => x + "|*." + x).
			Aggregate((x, y) => x + "|" + y);

		OpenFileDialog dlg = new()
		{
			Filter = String.Format("{0}|{1}", fdFilter1, fdFilter2),
			Multiselect = true
		};
		if (dlg.ShowDialog(this) != true)
			return;

		AddLocalizableResourceFiles(dlg.FileNames);
	}

	private void OnRemoveSelected(Object sender, ExecutedRoutedEventArgs e)
	{
		RemoveLocalizableResourceFiles(_localizableFilesDataGrid.SelectedItems.OfType<LocalizableResourceFile>());
	}

	private void OnClearFiles(Object sender, ExecutedRoutedEventArgs e)
	{
		ClearLocalizableResourceFiles();
	}

	private void OnSaveAll(Object sender, ExecutedRoutedEventArgs e)
	{
		SaveTranslationsTable(false);
	}

	private void OnAddLanguage(Object sender, ExecutedRoutedEventArgs e)
	{
		ComboBox? comboBoxLocales = null;
		LocaleInfo? addingLocaleInfo = null;

		// determine the LocaleInfo to be added
		DependencyObject? objParent = (DependencyObject)e.OriginalSource;
		while (objParent != null)
		{
			objParent = VisualTreeHelper.GetParent(objParent);
			if (objParent is DockPanel dockPanel)
			{
				foreach (UIElement objChild in dockPanel.Children)
				{
					if (objChild is ComboBox comboBox)
					{
						comboBoxLocales = comboBox;
						addingLocaleInfo = (LocaleInfo)comboBox.SelectedItem;
						break;
					}
				}
				break;
			}
		}

		// verify the selection
		if (addingLocaleInfo == null || !addingLocaleInfo.IsValid ||
			addingLocaleInfo.Culture == CultureInfo.InvariantCulture)
		{
			MessageBox.Show(Localization.UIMessagesSD.Instance.UIMessage_SelectLanguage,
				Localization.UIMessagesSD.Instance.MessageBox_Caption_Info, MessageBoxButton.OK, MessageBoxImage.Information);
			return;
		}

		// add the language
		AddLanguage(addingLocaleInfo);

		// reset combo box selection
		if (comboBoxLocales != null && comboBoxLocales.HasItems)
			comboBoxLocales.SelectedIndex = 0;
	}

	private void OnRemoveLanguage(Object sender, ExecutedRoutedEventArgs e)
	{
		String languageDisplayName = String.Empty;

		// determine the language to be removed
		DependencyObject? objParent = (DependencyObject)e.OriginalSource;
		while (objParent != null)
		{
			objParent = VisualTreeHelper.GetParent(objParent);
			if (objParent is DataGridColumnHeader columnHeader)
			{
				languageDisplayName = GetColumnHeaderText(columnHeader.Column);
				break;
			}
		}

		// translate to the languageDisplayName to LocaleInfo.Name
		TranslationsTable table = TranslationsTable;
		DataColumn? tableColumn = table.Columns.Cast<DataColumn>().FirstOrDefault(col => col.Caption == languageDisplayName);
		if (tableColumn == null)
			return;

		LocaleInfo removingLocaleInfo = new(tableColumn.ColumnName);
		if (!removingLocaleInfo.IsValid)
			return;

		// ask for confirmation to delete the column
		String msg = String.Format(Localization.UIMessagesSD.Instance.UIMessage_DeleteTransFor_Lang, languageDisplayName);
		if (MessageBox.Show(msg, Localization.UIMessagesSD.Instance.MessageBox_Caption_Warning,
			MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
		{
			return;
		}

		// remove it
		RemoveLanguage(removingLocaleInfo);
	}

	private void OnLocalizableFilesSelectionChanged(Object sender, SelectionChangedEventArgs e)
	{
		if (e.Source is DataGrid localizableFilesGrid)
			_ = LoadTranslationsTableAsync(localizableFilesGrid.SelectedItems.OfType<LocalizableResourceFile>());
	}

	private void OnTranslationsGridCellLoaded(Object sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox)
			DataObject.AddPastingHandler(textBox, OnTranslationsGridCellPaste);
	}

	private void OnTranslationsGridCellUnloaded(Object sender, RoutedEventArgs e)
	{
		if (sender is TextBox textBox)
			DataObject.RemovePastingHandler(textBox, OnTranslationsGridCellPaste);
	}

	private void OnTranslationsGridCellPaste(Object sender, DataObjectPastingEventArgs e)
	{
		if (sender is TextBox textBox && e.DataObject.GetDataPresent(DataFormats.UnicodeText, true))
		{
			String pastedText = (String)e.DataObject.GetData(DataFormats.UnicodeText, true);

			pastedText = pastedText.Trim();
			textBox.SelectedText = pastedText;
			textBox.CaretIndex += pastedText.Length;

			e.CancelCommand();
		}
	}

	private void OnTranslationsGridCellKeyDown(Object sender, System.Windows.Input.KeyEventArgs e)
	{
		if (e.Key == Key.Return && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
		{
			// Handle Ctrl+Return here
			if (sender is TextBox textBox)
			{
				textBox.SelectedText = Environment.NewLine;
				textBox.CaretIndex++;
				//textBox.SelectionLength = 0;
				e.Handled = true;
			}
		}
	}

	private void OnUILanguageSelectionChanged(Object sender, SelectionChangedEventArgs e)
	{
		if (e.AddedItems.Count > 0)
		{
			LocaleInfo? selectedLocale = e.AddedItems.Cast<LocaleInfo>().FirstOrDefault();
			if (selectedLocale != null)
			{
				if (selectedLocale.Culture != null)
				{
					Thread.CurrentThread.CurrentCulture = selectedLocale.Culture;
					Thread.CurrentThread.CurrentUICulture = selectedLocale.Culture;
				}
				else
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.InstalledUICulture;
					Thread.CurrentThread.CurrentUICulture = CultureInfo.InstalledUICulture;
				}

				LocalizationManager.Default.ChangeLocale(selectedLocale);
			}
		}
	}

	private void OnWindowClosing(Object sender, System.ComponentModel.CancelEventArgs e)
	{
		SaveTranslationsTable();
	}

	#endregion // UI command handlers

	private void ScanForLocalizableResourceFiles(String rootDirectoryPath, String[] fileTypes)
	{
		// check if there are any changes to apply back
		SaveTranslationsTable();

		if (fileTypes == null || fileTypes.Length == 0)
			return;

		IEnumerable<String> allFiles = fileTypes.
			SelectMany(ext => (IEnumerable<String>)System.IO.Directory.GetFiles(rootDirectoryPath, "*." + ext, System.IO.SearchOption.AllDirectories));

		// add directory contents to the list
		AddLocalizableResourceFiles(allFiles!);
	}
	private void AddLocalizableResourceFiles(IEnumerable<String> filePaths)
	{
		// check if there are any changes to apply back
		SaveTranslationsTable();

		// add selected files to the list
		foreach (String path in filePaths)
		{
			if (LocalizableFiles.Any(f => f.FullPath == path))
				continue;

			try
			{
				// add the file
				LocalizableResourceFile file = new();
				if (file.Load(path))
					LocalizableFiles.Add(file);
			}
			catch (Exception ex)
			{
				ReportFailure(ex);
			}
		}

		// update columns based on the list of localizable files
		UpdateTranslationsDataGridColumns();
	}

	private void RemoveLocalizableResourceFiles(IEnumerable<LocalizableResourceFile> selectedFiles)
	{
		// check if there are any changes to apply back
		SaveTranslationsTable();

		// remove the selected entries
		LocalizableResourceFile[] filesToRemove = selectedFiles.ToArray();
		foreach (LocalizableResourceFile file in filesToRemove)
			LocalizableFiles.Remove(file);

		// update columns based on the list of localizable files
		UpdateTranslationsDataGridColumns();
	}
	private void ClearLocalizableResourceFiles()
	{
		// check if there are any changes to apply back
		SaveTranslationsTable();

		// clear the list
		LocalizableFiles.Clear();

		// update columns based on the list of localizable files
		UpdateTranslationsDataGridColumns();
	}
	private void UpdateTranslationsDataGridColumns()
	{
		UpdateTranslationsDataGridColumns(true);
	}
	private async void UpdateTranslationsDataGridColumns(Boolean refreshTranslationsTable)
	{
		// ensure to have the table updated (it will add the appropriate columns)
		if (refreshTranslationsTable)
			await LoadTranslationsTableAsync(_localizableFilesDataGrid.SelectedItems.Cast<LocalizableResourceFile>()).ConfigureAwait(true);

		TranslationsTable table = TranslationsTable;

		if (table.Columns.Count > TranslationsTable.FixedColumnsCount)
		{
			for (Int32 columnIndex = TranslationsTable.FixedColumnsCount; columnIndex < table.Columns.Count; columnIndex++)
			{
				DataColumn tableCol = table.Columns[columnIndex];

				// check if the column already exists
				if (_translationsDataGrid.Columns.Any(col => GetColumnHeaderText(col) == tableCol.Caption))
					continue;

				// add new column
				DataGridTextColumn dataGridCol = new()
				{
					Header = tableCol.Caption,
					HeaderTemplate = (DataTemplate)Resources["TranslationsColumnHeaderDataTemplate"],
					HeaderStyle = (Style)Resources["TranslationsDataGridColumnHeaderStyle"],
					// IMPORTANT: Use bracket notation to support names like "en-US"
					Binding = new Binding() { Path = new PropertyPath("[" + tableCol.ColumnName + "]") },
					ElementStyle = (Style)Resources["TranslationsDataGridReaderStyle"],
					EditingElementStyle = (Style)Resources["TranslationsDataGridEditorStyle"]
				};

				// insert before the "Add Language" column
				_translationsDataGrid.Columns.Insert(_translationsDataGrid.Columns.Count - 1, dataGridCol);
			}
		}

		// remove unused columns (ensure to keep the last "Add Language" column)
		for (Int32 columnIndex = _translationsDataGrid.Columns.Count - 2; columnIndex >= TranslationsTable.FixedColumnsCount; columnIndex--)
		{
			DataGridColumn dataGridCol = _translationsDataGrid.Columns[columnIndex];
			if (!table.Columns.OfType<DataColumn>().Any(tableCol => tableCol.Caption == GetColumnHeaderText(dataGridCol)))
				_translationsDataGrid.Columns.RemoveAt(columnIndex);
		}
	}

	private CancellationTokenSource? _loadTranslationsCancellationTokenSource = null;
	private static readonly Mutex _fileLoadSaveMutex = new(false);
	private async Task LoadTranslationsTableAsync(IEnumerable<LocalizableResourceFile> selectedFiles)
	{
		// display loading progress indicator
		_progressLoading.Visibility = Visibility.Visible;

		TranslationsTable? table = null;

		// cancel previous loading and create a fresh token for the current load request
		_loadTranslationsCancellationTokenSource?.Cancel(false);
		_loadTranslationsCancellationTokenSource = new CancellationTokenSource();

		CancellationTokenSource cancellationTokenSource = _loadTranslationsCancellationTokenSource;

		try
		{
			// Fill Translations table with the selected Localizable Resource Files
			table = await LoadTranslationsTableAsync(selectedFiles, LocalizableFiles, TranslationsTable, _loadTranslationsCancellationTokenSource.Token).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			ReportFailure(ex);
		}

		Boolean isFinishedSuccessfully = !cancellationTokenSource.IsCancellationRequested;
		if (cancellationTokenSource == _loadTranslationsCancellationTokenSource)
			_loadTranslationsCancellationTokenSource = null;
		cancellationTokenSource.Dispose();
		if (!isFinishedSuccessfully)
			return;

		// update the data grid
		TranslationsTable = table ?? new TranslationsTable();

		// hide the progress
		_progressLoading.Visibility = Visibility.Hidden;
	}
	private static async Task<TranslationsTable?> LoadTranslationsTableAsync(IEnumerable<LocalizableResourceFile> selectedFiles,
		IEnumerable<LocalizableResourceFile> allFiles, TranslationsTable? currentTable, CancellationToken token)
	{
		return await Task.Run(() =>
		{
			if (!_fileLoadSaveMutex.WaitOne())
				return null;

			try
			{
				return LoadTranslationsTableAsyncUnsafe(selectedFiles, allFiles, currentTable, token);
			}
			finally
			{
				_fileLoadSaveMutex.ReleaseMutex();
			}
		}).ConfigureAwait(true);
	}
	private static async Task<TranslationsTable?> LoadTranslationsTableAsyncUnsafe(IEnumerable<LocalizableResourceFile> selectedFiles,
		IEnumerable<LocalizableResourceFile> allFiles, TranslationsTable? currentTable, CancellationToken token)
	{
		// check if there are any changes to apply back
		if (currentTable != null)
			await SaveTranslationsTableAsync(allFiles, currentTable, true, token).ConfigureAwait(false);

		if (token.IsCancellationRequested)
			return null;

		// create the table
		TranslationsTable table = new();
		table.CreateLocaleColumns(allFiles);

		// check if there's any selection
		if (!selectedFiles.Any())
			return table;

		// fill the data
		foreach (LocalizableResourceFile locFile in selectedFiles)
		{
			if (token.IsCancellationRequested)
				return null;

			LocalizationManager? lm = locFile.LocalizationManager;
			IEnumerable<KeyValuePair<String, String>>? contents = null;

			try
			{
				contents = locFile.GetNativeContents();
			}
			catch (Exception ex)
			{
				ReportFailure(ex);
				continue;
			}
			if (lm == null || contents == null)
				continue;

			IEnumerable<LocaleInfo> locales = lm.AllLocales;

			// fill standard columns data
			Dictionary<String, DataRow> resKeyRowsMap = new();
			foreach (KeyValuePair<String, String> pair in contents)
			{
				if (token.IsCancellationRequested)
					return null;

				DataRow dataRow = table.NewRow();

				dataRow[table.ResourceFileIdColumn] = locFile.Id;
				dataRow[table.ResourceFileNameColumn] = locFile.FileName;
				dataRow[table.ResourceKeyColumn] = pair.Key;
				dataRow[table.NativeValueColumn] = pair.Value;

				table.Rows.Add(dataRow);
				resKeyRowsMap.Add(pair.Key, dataRow);
			}

			// fill translation columns data
			foreach (LocaleInfo locale in locales)
			{
				if (token.IsCancellationRequested)
					return null;

				// get translations for the current locale
				IEnumerable<KeyValuePair<String, String>>? translations = null;
				try
				{
					translations = locFile.GetTranslations(locale);
				}
				catch (Exception ex)
				{
					ReportFailure(ex);
					continue;
				}
				if (translations == null)
					continue;

				// find table column
				Int32 colIndex = table.Columns.IndexOf(locale.Name);
				if (colIndex == -1)
					continue;

				// fill the column data
				foreach (KeyValuePair<String, String> pair in translations)
				{
					if (token.IsCancellationRequested)
						return null;

					if (resKeyRowsMap.TryGetValue(pair.Key, out DataRow? dataRow) && dataRow != null)
						dataRow[colIndex] = pair.Value;
				}
			}
		}

		// mark table unchanged
		table.AcceptChanges();

		return table;
	}

	private void SaveTranslationsTable()
	{
		SaveTranslationsTable(true);
	}
	private async void SaveTranslationsTable(Boolean saveChangesOnly)
	{
		_ = await SaveTranslationsTableAsync(LocalizableFiles, TranslationsTable, saveChangesOnly, CancellationToken.None).ConfigureAwait(true);
	}
	private static async Task<Boolean> SaveTranslationsTableAsync(IEnumerable<LocalizableResourceFile> localizableFiles, TranslationsTable table, Boolean saveChangesOnly, CancellationToken token)
	{
		return await Task.Run(() =>
		{
			if (!_fileLoadSaveMutex.WaitOne())
				return false;

			try
			{
				return SaveTranslationsTableAsyncUnsafe(localizableFiles, table, saveChangesOnly, token);
			}
			finally
			{
				_fileLoadSaveMutex.ReleaseMutex();
			}
		}).ConfigureAwait(true);
	}
	private static Boolean SaveTranslationsTableAsyncUnsafe(IEnumerable<LocalizableResourceFile> localizableFiles, TranslationsTable table, Boolean saveChangesOnly, CancellationToken token)
	{
		DataTable contentsToSave;

		if (token.IsCancellationRequested)
			return false;

		if (saveChangesOnly)
		{
			DataTable? changes = table.GetChanges();
			if (changes == null || changes.Rows.Count == 0)
				return true;

			contentsToSave = changes;
		}
		else
		{
			// save the whole table
			contentsToSave = table;
		}

		for (Int32 column = TranslationsTable.FixedColumnsCount; column < table.Columns.Count; column++)
		{
			if (token.IsCancellationRequested)
				return false;

			// get the current locale
			LocaleInfo locale = new(table.Columns[column].ColumnName);
			if (!locale.IsValid)
				continue;

			// load current locale translations from data table
			Dictionary<Guid, List<KeyValuePair<String, String>>> mapChangesPerFile = new();
			List<KeyValuePair<String, String>>? listTranslations = null;

			foreach (DataRow dataRow in contentsToSave.Rows)
			{
				if (token.IsCancellationRequested)
					return false;

				//if ((String)dataRow[column] == (String)dataRow[column, DataRowVersion.Original])
				//    continue;

				Guid localizableFileId = (Guid)dataRow[table.ResourceFileIdColumn.Ordinal];
				String resourceKey = (String)dataRow[table.ResourceKeyColumn.Ordinal];
				String resourceValue = dataRow.IsNull(column) ? String.Empty : (String)dataRow[column];
				KeyValuePair<String, String> transRecord = new(resourceKey, resourceValue);

				if (!mapChangesPerFile.TryGetValue(localizableFileId, out listTranslations))
					mapChangesPerFile.Add(localizableFileId, listTranslations = new List<KeyValuePair<String, String>>());
				listTranslations.Add(transRecord);
			}

			// save translation files
			foreach (KeyValuePair<Guid, List<KeyValuePair<String, String>>> transFile in mapChangesPerFile)
			{
				LocalizableResourceFile? file = localizableFiles.FirstOrDefault(f => f != null && f.Id == transFile.Key, null);
				if (file == null)
					continue;

				IEnumerable<KeyValuePair<String, String>> translations = transFile.Value;
				file.SaveTranslations(locale, translations);
			}
		}

		// mark table unchanged
		table.AcceptChanges();

		return true;
	}

	private void AddLanguage(LocaleInfo localeInfo)
	{
		if (!localeInfo.IsValid)
			return;

		// create translations for all loaded localizable files
		foreach (LocalizableResourceFile lf in LocalizableFiles)
		{
			try
			{
				lf.CreateTranslation(localeInfo);
			}
			catch (Exception ex)
			{
				ReportFailure(ex);
			}
		}

		// update columns
		UpdateTranslationsDataGridColumns();
	}
	private void RemoveLanguage(LocaleInfo localeInfo)
	{
		if (!localeInfo.IsValid)
			return;

		// create translations for all loaded localizable files
		foreach (LocalizableResourceFile lf in LocalizableFiles)
		{
			try
			{
				lf.DeleteTranslation(localeInfo);
			}
			catch (Exception ex)
			{
				ReportFailure(ex);
			}
		}

		// update columns
		UpdateTranslationsDataGridColumns();
	}

	private static String GetColumnHeaderText(DataGridColumn col)
	{
		if (col.Header is String s)
			return s;

		if (col.Header is TextBlock t)
			return t.Text;

		return String.Empty;
	}
	private static void ReportFailure(Exception ex)
	{
		MessageBox.Show(ex.Message, Localization.UIMessagesSD.Instance.MessageBox_Caption_Error, MessageBoxButton.OK, MessageBoxImage.Error);
	}
}
