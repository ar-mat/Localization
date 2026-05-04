using System;
using System.Collections.Generic;

namespace Armat.Localization;

public record struct Configuration : IEquatable<Configuration>
{
	public Configuration()
	{
		SupportedLocales = null;
		DefaultLocale = null;
		TranslationsDirectoryPath = String.Empty;
		TranslationLoadBehavior = TranslationLoadBehavior.KeepNative;
	}

	private static readonly Configuration _default = new()
	{
		SupportedLocales = null,
		DefaultLocale = LocaleInfo.Invalid,
		TranslationsDirectoryPath = "Localization",
		TranslationLoadBehavior = TranslationLoadBehavior.KeepNative
	};
	public static Configuration Default => _default;

	public IEnumerable<LocaleInfo>? SupportedLocales { get; set; }
	public LocaleInfo? DefaultLocale { get; set; }
	public String TranslationsDirectoryPath { get; set; }
	public TranslationLoadBehavior TranslationLoadBehavior { get; set; }

}

public enum TranslationLoadBehavior
{
	KeepNative,
	ClearNative,
	RemoveNative
}
