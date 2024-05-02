using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize enums.
/// </summary>
public class EnumConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets whether this converter should serialize enum values into their
	/// string representation or numeric ones.
	/// </summary>
	public static bool EnumToString { get; set; } = false;

	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type)
	{
		return type.IsEnum;
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		if (value.IsInteger)
		{
			return Enum.ToObject(requestedType, value.GetInteger());
		}
		else
		{
			return Enum.Parse(requestedType, value.GetString(), true);
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		if (EnumToString)
		{
			return new JsonValue(value.ToString()!);
		}
		else
		{
			return new JsonValue((int)value);
		}
	}
}

