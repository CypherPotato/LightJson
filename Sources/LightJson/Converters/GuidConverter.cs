using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an <see cref="JsonConverter"/> which can serialize and deserialize <see cref="Guid"/> values.
/// </summary>
public class GuidConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type)
	{
		return type == typeof(Guid);
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType)
	{
		return Guid.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value)
	{
		return new JsonValue(value.ToString()!);
	}
}
