using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="DateOnly"/> values.
/// </summary>
public sealed class DateOnlyConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets the <see cref="IFormatProvider"/> used to serialize and deserialize values.
	/// </summary>
	public static IFormatProvider FormatProvider { get; set; } = System.Globalization.CultureInfo.InvariantCulture;

	/// <summary>
	/// Gets or sets the exact format for serializing and deserializing values.
	/// </summary>
	public static string Format { get; set; } = "yyyy-MM-dd";

	/// <inheritdoc/>
	public override bool CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(DateOnly);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return DateOnly.ParseExact(value.GetString(), Format, FormatProvider);
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		DateOnly d = (DateOnly)value;
		return new JsonValue(d.ToString(Format, FormatProvider));
	}
}
