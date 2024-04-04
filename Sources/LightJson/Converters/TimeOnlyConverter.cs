using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		return new TimeOnly(value.GetLong());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		TimeOnly d = (TimeOnly)value;
		return new JsonValue(d.Ticks);
	}
}
