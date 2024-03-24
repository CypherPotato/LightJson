using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using LightJson.Converters;

namespace LightJson;

#nullable enable

/// <summary>
/// Provides options and configurations for using the LightJson library.
/// </summary>
public static class JsonOptions
{
	/// <summary>
	/// Gets or sets a value that indicates whether a property's name uses a case-insensitive comparison when getting
	/// values.
	/// </summary>
	public static bool PropertyNameCaseInsensitive { get; set; } = false;

	/// <summary>
	/// Gets or sets whether the <see cref="JsonValue.FromObject(object?, bool)"/> should serialize fields from
	/// types.
	/// </summary>
	public static bool SerializeFields { get; set; } = false;

	/// <summary>
	/// Gets or sets an list of <see cref="JsonConverter"/>.
	/// </summary>
	public static HashSet<JsonConverter> Converters { get; set; } = new HashSet<JsonConverter>()
	{
		new DictionaryConverter(),
		new GuidConverter(),
		new EnumConverter(),
		new DatetimeConverter()
	};

	/// <summary>
	/// Gets or sets the function that transforms the property name of a JSON object to JSON output.
	/// </summary>
	public static JsonNamingPolicy? NamingPolicy { get; set; }
}
