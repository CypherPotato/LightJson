using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="IDictionary{TKey, TValue}"/> values.
/// </summary>
public sealed class DictionaryConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type.IsAssignableTo(typeof(IDictionary<string, object?>));
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		if (requestedType == typeof(ExpandoObject))
		{
			return this.JsonValueToObject(value, 0, currentOptions)!;
		}
		else
		{
			return value.GetJsonObject().Properties;
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		JsonObject result = new JsonObject(currentOptions);
		IDictionary<string, object?> items = (IDictionary<string, object?>)value;

		foreach (var item in items)
		{
			result.Add(item.Key, JsonValue.Serialize(item.Value));
		}

		return result.AsJsonValue();
	}

	object? JsonValueToObject(JsonValue self, int deepness, JsonOptions currentOptions)
	{
		if (deepness > currentOptions.DynamicObjectMaxDepth)
		{
			throw new ArgumentOutOfRangeException("The JSON deserialization reached it's maximum depth.");
		}

		switch (self.Type)
		{
			case JsonValueType.Array:
				return self.GetJsonArray()
					.Select(n => this.JsonValueToObject(n, deepness + 1, currentOptions))
					.ToArray();

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
					expando.Add(kvp.Key, this.JsonValueToObject(kvp.Value, deepness + 1, currentOptions));
				}
				return expando;

			default: // covers null and undefined
				return null;
		}
	}
}
