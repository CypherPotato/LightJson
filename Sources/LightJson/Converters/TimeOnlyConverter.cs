using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="TimeOnly"/> values.
/// </summary>
public sealed class TimeOnlyConverter : JsonConverter
{
	/// <inheritdoc/>
	public override bool CanSerialize(Type type)
	{
		return type == typeof(TimeOnly);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		return TimeOnly.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		TimeOnly d = (TimeOnly)value;
		return new JsonValue(d.ToLongTimeString());
	}
}
