using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="IDictionary{TKey, TValue}"/> values.
/// </summary>
public class DictionaryConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type)
	{
		return type.IsAssignableTo(typeof(IDictionary<string, object?>));
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		if (requestedType == typeof(ExpandoObject))
		{
			return JsonValueToObject(value, 0)!;
		}
		else
		{
			return value.GetJsonObject().Properties;
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value)
	{
		JsonObject result = new JsonObject(base.CurrentOptions);
		IDictionary<string, object?> items = (IDictionary<string, object?>)value;

		foreach (var item in items)
		{
			result.Add(item.Key, JsonValue.Serialize(item.Value));
		}

		return result.AsJsonValue();
	}

	object? JsonValueToObject(JsonValue self, int deepness)
	{
		if (deepness > CurrentOptions.DynamicObjectMaxDepth)
		{
			throw new ArgumentOutOfRangeException("The JSON deserialization reached it's maximum depth.");
		}

		switch (self.Type)
		{
			case JsonValueType.Array:
				return self.GetJsonArray()
					.Select(n => JsonValueToObject(n, deepness + 1));

			case JsonValueType.String:
				return self.GetString();

			case JsonValueType.Number:
				return self.GetNumber();

			case JsonValueType.Boolean:
				return self.GetBoolean();

			case JsonValueType.Object:
				var jobj = self.GetJsonObject();
				IDictionary<string, object?> expando = new ExpandoObject();
				foreach (var kvp in jobj)
				{
					expando.Add(kvp.Key, JsonValueToObject(kvp.Value, deepness + 1));
				}
				return expando;

			default: // covers null and undefined
				return null;
		}
	}
}
