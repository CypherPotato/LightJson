using LightJson.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LightJson.Serialization;
static class JsonSerializer
{
	public static JsonValue SerializeObject(object? value, int deepness, bool convertersEnabled, JsonOptions options)
	{
		if (deepness > options.DynamicObjectMaxDepth)
		{
			throw new JsonException("The JSON serialization reached it's maximum depth.");
		}
		if (value is null)
		{
			return new JsonValue(JsonValueType.Null, 0, null, options);
		}

		var itemType = value.GetType();

		if (value is JsonValue jval)
		{
			return jval;
		}
		else if (value is IImplicitJsonValue implicitJval)
		{
			JsonValue result = implicitJval.AsJsonValue();
			return result;
		}
		else if (value is string || value is char)
		{
			return new JsonValue(JsonValueType.String, 0, value.ToString(), options);
		}
		else if (value is int nint)
		{
			return new JsonValue(JsonValueType.Number, nint, null, options);
		}
		else if (value is byte || value is sbyte || value is uint || value is long || value is ulong)
		{
			return new JsonValue(JsonValueType.Number, Convert.ToInt64(value), null, options);
		}
		else if (value is double ndbl)
		{
			return new JsonValue(JsonValueType.Number, ndbl, null, options);
		}
		else if (value is float || value is decimal)
		{
			return new JsonValue(JsonValueType.Number, Convert.ToDouble(value), null, options);
		}
		else if (value is bool nbool)
		{
			return new JsonValue(JsonValueType.Boolean, nbool ? 1 : 0, null, options);
		}

		IList<JsonConverter> converters =
			convertersEnabled ?
			[.. options.Converters, .. JsonOptions.RequiredConverters] :
			JsonOptions.RequiredConverters;

		for (int i = 0; i < converters.Count; i++)
		{
			JsonConverter mapper = converters[i];
			if (mapper.CanSerialize(itemType, options))
			{
				try
				{
					var result = mapper.Serialize(value, options);
					return result;
				}
				catch (Exception ex)
				{
					throw new JsonException($"Unhandled exception while trying to serialize the JSON value through {mapper.GetType().Name}: {ex.Message}", ex);
				}
			}
		}

		if (JsonSerializableHelpers.TryDynamicSerialize(value, itemType, options, out var dynamicResult))
		{
			return dynamicResult;
		}

		if (options.SerializerContext is { } jcontext &&
			TrySerializeObjectWithTypeInfo(value, deepness, convertersEnabled, jcontext.TypeResolver, jcontext.SerializerOptions, options, out var typeInfoResult))
		{
			return typeInfoResult;
		}

		throw new JsonException($"Unable to serialize the JSON value of type {value.GetType().Name}: no converter matched the specified type.");
	}

	static bool TrySerializeObjectWithTypeInfo(object value, int deepness, bool convertersEnabled, IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions, JsonOptions options, out JsonValue result)
	{
		var typeInfo = typeResolver.GetTypeInfo(value.GetType(), serializerOptions);
		switch (typeInfo?.Kind)
		{
			case JsonTypeInfoKind.Object:
				{
					var obj = options.CreateJsonObject();
					foreach (var prop in typeInfo.Properties)
					{
						if (prop.Get is null)
							continue;

						var propValue = prop.Get(value);

						if (prop.ShouldSerialize is { } shouldSerialize && !shouldSerialize(value, propValue))
						{
							continue;
						}
						var propValueJson = SerializeObject(propValue, deepness + 1, convertersEnabled, options);
						obj.Add(prop.Name, propValueJson);
					}

					result = obj;
					return true;
				}

			case JsonTypeInfoKind.Enumerable:
				{
					var arr = options.CreateJsonArray();
					foreach (var item in (IEnumerable)value)
					{
						var itemJson = SerializeObject(item, deepness + 1, convertersEnabled, options);
						arr.Add(itemJson);
					}

					result = arr;
					return true;
				}

			case JsonTypeInfoKind.Dictionary:
				{
					var obj = options.CreateJsonObject();

					void PushEntry(object? entryKey, object? entryValue)
					{
						string? ikey = entryKey?.ToString();

						if (string.IsNullOrEmpty(ikey))
							throw new JsonException($"The dictionary key is null.");

						JsonValue valueSerialized = SerializeObject(entryValue, deepness + 1, convertersEnabled, options);
						obj.Add(ikey, valueSerialized);
					}

					if (value is IDictionary genericDictionary)
					{
						foreach (DictionaryEntry entry in genericDictionary)
						{
							PushEntry(entry.Key, entry.Value);
						}
					}
					else if (value is IDictionary<string, object?> expando)
					{
						foreach (KeyValuePair<string, object?> entry in expando)
						{
							PushEntry(entry.Key, entry.Value);
						}
					}

					result = obj;
					return true;
				}
		}

		result = JsonValue.Undefined;
		return false;
	}
}
