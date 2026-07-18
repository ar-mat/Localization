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

	private static readonly LocaleInfo _invalid = new() { DisplayNameOverride = "[Native]" };
	public static LocaleInfo Invalid { get => _invalid; }

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


	// equality is based on the culture name only - two LocaleInfo instances for the
	// same culture must compare equal regardless of DisplayNameOverride
	public virtual Boolean Equals(LocaleInfo? other)
	{
		if (other is null)
			return false;

		return Name == other.Name;
	}
	public override Int32 GetHashCode()
	{
		return Name.GetHashCode();
	}

	// comparison by names allows getting a sorted list of all locales to be displayed on locale selectors
	public Int32 CompareTo(LocaleInfo? other)
	{
		if (other == null)
			return 1;

		return String.CompareOrdinal(Name, other.Name);
	}
	public override String ToString()
	{
		return DisplayName;
	}
}
