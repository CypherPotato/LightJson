using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <inheritdoc/>
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
