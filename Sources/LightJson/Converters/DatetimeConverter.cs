using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <inheritdoc/>
public class DatetimeConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets the <see cref="DateTime.ToString()"/> serialize format.
	/// </summary>
	public static string Format { get; set; } = "s";

	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type)
	{
		return type == typeof(DateTime);
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType)
	{
		return DateTime.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value)
	{
		DateTime t = (DateTime)value;
		return new JsonValue(t.ToString(Format));
	}
}
