using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="DateTime"/> values.
/// </summary>
public class DateTimeConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets the <see cref="DateTime.ToString()"/> serialize format.
	/// </summary>
	public static string Format { get; set; } = "s";

	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(DateTime);
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return DateTime.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value, JsonOptions currentOptions)
	{
		DateTime t = (DateTime)value;
		return new JsonValue(t.ToString(Format));
	}
}
