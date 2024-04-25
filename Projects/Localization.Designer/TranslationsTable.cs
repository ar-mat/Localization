using System;
using System.Collections.Generic;
using System.Data;

namespace Armat.Localization.Designer;

public class TranslationsTable : DataTable
{
	public TranslationsTable() : base(nameof(TranslationsTable))
	{
		// create standard columns
		Columns.AddRange(new DataColumn[]
		{
			ResourceFileIdColumn = new DataColumn("ResourceFileId", typeof(Guid)),
			ResourceFileNameColumn = new DataColumn("ResourceFileName", typeof(String)),
			ResourceKeyColumn = new DataColumn("ResourceKey", typeof(String)),
			NativeValueColumn = new DataColumn("NativeValue", typeof(String))
		});
	}

	public const Int32 FixedColumnsCount = 4;

	public DataColumn ResourceFileIdColumn { get; init; }
	public DataColumn ResourceFileNameColumn { get; init; }
	public DataColumn ResourceKeyColumn { get; init; }
	public DataColumn NativeValueColumn { get; init; }

	public void CreateLocaleColumns(IEnumerable<LocalizableResourceFile> allFiles)
	{
		// create translation columns
		foreach (LocalizableResourceFile locFile in allFiles)
		{
			LocalizationManager? lm = locFile.LocalizationManager;
			if (lm != null)
				CreateLocaleColumns(lm.AllLocales);
		}
	}
	public void CreateLocaleColumns(IEnumerable<LocaleInfo> locales)
	{
		// create localization columns
		foreach (LocaleInfo locale in locales)
		{
			if (Columns.IndexOf(locale.Name) == -1)
				Columns.Add(new DataColumn(locale.Name, typeof(String)) { Caption = locale.DisplayName });
		}
	}
}
