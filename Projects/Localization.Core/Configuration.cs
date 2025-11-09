using System;

namespace Armat.Localization;

public record struct Configuration : IEquatable<Configuration>
{
	public Configuration()
	{
		DefaultLocale = null;
		TranslationsDirectoryPath = String.Empty;
		TranslationLoadBehavior = TranslationLoadBehavior.KeepNative;
	}

	private static readonly Configuration _default = new()
	{
		DefaultLocale = LocaleInfo.Invalid,
		TranslationsDirectoryPath = "Localization",
		TranslationLoadBehavior = TranslationLoadBehavior.KeepNative
	};
	public static Configuration Default => _default;

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
