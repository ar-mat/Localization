using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Armat.Localization;

public record LocaleInfo : IComparable<LocaleInfo>
{
	private LocaleInfo()
	{
	}
	public LocaleInfo(CultureInfo culture)
	{
		Culture = culture;
	}
	public LocaleInfo(CultureInfo culture, String displayNameOverride)
	{
		Culture = culture;
		DisplayNameOverride = displayNameOverride;
	}
	public LocaleInfo(String localeName)
	{
		try
		{
			Culture = CultureInfo.GetCultureInfo(localeName);
		}
		catch (CultureNotFoundException)
		{
		}
	}

	public static readonly LocaleInfo Invalid = new() { DisplayNameOverride = "[Native]" };
	public static IEnumerable<LocaleInfo> AllLocales
	{
		get => CultureInfo.GetCultures(CultureTypes.AllCultures).
			Select(c => new LocaleInfo(c)).
			OrderBy(loc => loc.DisplayName);
	}

	public CultureInfo? Culture { get; }
	public String? DisplayNameOverride { get; init; }

	public Boolean IsValid => Culture != null && Culture != CultureInfo.InvariantCulture;

	public String Name
	{
		get
		{
			if (Culture == null)
				return String.Empty;

			return Culture.Name;
		}
	}
	public String DisplayName
	{
		get
		{
			if (DisplayNameOverride != null)
				return DisplayNameOverride;

			if (Culture == null)
				return String.Empty;

			return Culture.DisplayName;
		}
	}

	// comparison by names allows getting a sorted list of all locales to be displayed on locale selectors
	public Int32 CompareTo(LocaleInfo? other)
	{
		if (other == null)
			return 1;

		return Name.CompareTo(other.Name);
	}
	public override String ToString()
	{
		return DisplayName;
	}
}
