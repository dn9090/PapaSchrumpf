using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Data;

namespace PapaSchrumpf;

public class Translator : IValueConverter
{
	internal static Dictionary<string, string> translations = new Dictionary<string, string>();

	public static string locale
	{
		get => Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
		set => Thread.CurrentThread.CurrentUICulture = new CultureInfo(value);
	}

	public static void Load(Stream stream)
	{
		var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(stream);
	
		if(!dict.TryGetValue("locale", out var locale))
			throw new InvalidOperationException();

		locale = locale.ToLower();

		foreach(var kv in dict)
			translations.Add(locale + "." + kv.Key, kv.Value);
	}

	public static string T(string key)
	{
		if(string.IsNullOrEmpty(key) || key[0] != '#')
			return key;

		var normalizedKey = key.Substring(1, key.Length - 1);

		if(translations.TryGetValue(locale + "." + normalizedKey, out var value))
			return value;
		
		return "(unknown localization)";
	}

	public Translator()
	{
	}

	public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		if(value == null)
			return DependencyProperty.UnsetValue;

		if(value is string str)
			return T(str);
			
		return value;
	}

	public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
	{
		return Binding.DoNothing;
	}
}
