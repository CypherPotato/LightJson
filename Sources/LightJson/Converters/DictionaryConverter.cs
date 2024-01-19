using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <inheritdoc/>
public class DictionaryConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type)
	{
		return type.IsAssignableTo(typeof(IDictionary<string, object?>));
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType)
	{
		return value.GetJsonObject().Properties;
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value)
	{
		JsonObject result = new JsonObject();
		IDictionary<string, object?> items = (IDictionary<string, object?>)value;

		foreach (var item in items)
		{
			result.Add(item.Key, JsonValue.FromObject(item.Value));
		}

		return result.AsJsonValue();
	}
}
