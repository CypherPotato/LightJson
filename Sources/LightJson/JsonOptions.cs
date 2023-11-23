using System;
using System.Collections.Generic;
using System.Text;

namespace LightJson;

public static class JsonOptions
{
	/// <summary>
	/// Gets or sets a value that indicates whether a property's name uses a case-insensitive comparison when getting
	/// values.
	/// </summary>
	public static bool PropertyNameCaseInsensitive { get; set; } = false;

	/// <summary>
	/// Gets or sets whether the JSON serializer should throw an exception when trying to convert a value to an invalid type.
	/// </summary>
	public static bool ThrowOnInvalidCast { get; set; } = false;
}
