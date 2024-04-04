using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="TimeSpan"/> values.
/// </summary>
public sealed class TimeSpanConverter : JsonConverter
{
	/// <inheritdoc />
	public override bool CanSerialize(Type type)
	{
		return type == typeof(TimeSpan);
	}

	/// <inheritdoc />
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		return new TimeSpan(value.GetLong());
	}

	/// <inheritdoc />
	public override JsonValue Serialize(object value)
	{
		TimeSpan s = (TimeSpan)value;
		return new JsonValue(s.Ticks);
	}
}
