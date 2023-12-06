using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <inheritdoc/>
public class EnumConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets whether this converter should convert enum values into their
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

