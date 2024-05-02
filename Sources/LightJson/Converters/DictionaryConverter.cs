using System;
using System.Collections.Generic;

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
	public override Object Deserialize(JsonValue value, Type requestedType)
	{
		return value.GetJsonObject().Properties;
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
}
