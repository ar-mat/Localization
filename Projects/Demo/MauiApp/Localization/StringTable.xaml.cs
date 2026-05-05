using Armat.Localization.Maui;

using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;

using System;
using System.Collections.Generic;

namespace Armat.Localization.Demo.Localization;

public partial class StringTable : LocalizableResourceDictionary
{
	public StringTable()
	{
	}

	//public String GetClickMeCounterText(Int32 count)
	//{
	//	String counterFormat = this["Btn_ClickMe_Counter"] as String
	//		?? throw new KeyNotFoundException("Btn_ClickMe_Counter");

	//	return String.Format(counterFormat, count);
	//}
}