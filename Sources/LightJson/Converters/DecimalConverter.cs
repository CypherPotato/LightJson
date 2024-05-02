using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <summary>
/// Represents an <see cref="JsonConverter"/> which can serialize and deserialize <see cref="decimal"/> values.
/// </summary>
public class DecimalConverter : JsonConverter
{
	/// <inheritdoc />
	public override bool CanSerialize(Type type)
	{
		return type == typeof(decimal);
	}

	/// <inheritdoc />
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		return (decimal)value.GetNumber();
	}

	/// <inheritdoc />
	public override JsonValue Serialize(object value)
	{
		return new JsonValue((double)(decimal)value);
	}
}
