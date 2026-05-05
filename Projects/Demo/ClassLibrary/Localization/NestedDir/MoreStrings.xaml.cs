using System;
using System.Collections.Generic;
using System.Text;

namespace Armat.Localization.Demo.Lib.Localization.NestedDir;

internal class MoreStrings : LocalizableStringDictionary
{
	public MoreStrings() :
		this(LocalizableStringDictionary.FormResourceUri(typeof(MoreStrings)))
	{
	}
	public MoreStrings(Uri sourceUri)
	{
		LoadNative(sourceUri, LocalizationManager.Default);
	}

	public String FirstString => this["Key_String_1"];
	public String SecondString => this["Key_String_2"];
}
