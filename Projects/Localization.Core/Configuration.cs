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

	public static readonly Configuration Default = new()
	{
		DefaultLocale = LocaleInfo.Invalid,
		TranslationsDirectoryPath = "Localization",
		TranslationLoadBehavior = TranslationLoadBehavior.KeepNative
	};

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
