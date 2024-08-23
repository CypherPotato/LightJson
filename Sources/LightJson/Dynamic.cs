using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace LightJson;

internal class Dynamic
{
	public static object DeserializeObject(JsonValue value, Type tinputType, JsonOptions options)
	{
		string SanitizeCharcase(string s)
		{
			return new string(s.Where(char.IsLetterOrDigit).ToArray());
		}

		bool ICharcaseCompare(string a, string b)
		{
			string A = SanitizeCharcase(a);
			string B = SanitizeCharcase(b);
			return string.Compare(A, B, true) == 0;
		}

		object DeserializeObjectX(JsonValue value, Type type, int deepness, JsonOptions options)
		{
			if (type == typeof(int) || type == typeof(uint) ||
				type == typeof(long) || type == typeof(ulong) ||
				type == typeof(double) || type == typeof(float) ||
				type == typeof(byte) || type == typeof(sbyte))
			{
				return Convert.ChangeType(value.GetNumber(), type);
			}
			else if (type == typeof(string))
			{
				return value.GetString();
			}
			else if (type == typeof(bool))
			{
				return value.GetBoolean();
			}
			else if (type == typeof(JsonObject))
			{
				return value.GetJsonObject();
			}
			else if (type == typeof(JsonArray))
			{
				return value.GetJsonArray();
			}
			else
			{
				for (int i = 0; i < options.Converters.Count; i++)
				{
					var mapper = options.Converters[i];
					if (mapper.CanSerialize(type))
					{
						try
						{
							return mapper.Deserialize(value, type);
						}
						catch (Exception ex)
						{
							throw new InvalidOperationException($"Caught exception while trying to map {value.path} to {type.Name}: {ex.Message}");
						}
					}
				}

				if (type.IsAssignableTo(typeof(IEnumerable)))
				{
					Type subType = type.GetGenericArguments()[0];
					var arr = value.GetJsonArray();

					ArrayList items = new ArrayList();
					for (int i = 0; i < arr.Count; i++)
					{
						items.Add(DeserializeObjectX(value, subType, deepness + 1, options));
					}

					if (type.IsAssignableTo(typeof(ICollection<>)))
					{
						return items.ToArray().ToList();
					}
					else
					{
						return items.ToArray();
					}
				}
				else
				{
					PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
					var jobj = value.GetJsonObject();

					object? objInstance = Activator.CreateInstance(type);
					if (objInstance is null)
					{
						throw new InvalidOperationException($"The JSON deserializer couldn't create an instance of {type.Name}.");
					}

					for (int i = 0; i < properties.Length; i++)
					{
						var prop = properties[i];

						string propName = prop.Name;
						JsonValue? jobjChild = jobj
							.Properties
								.Where(p => ICharcaseCompare(propName, p.Key))
								.Select(p => p.Value)
								.Cast<JsonValue?>()
								.FirstOrDefault();

						if (jobjChild is JsonValue jvalue)
						{
							if (prop.GetCustomAttribute<JsonIgnoreAttribute>() is JsonIgnoreAttribute ignore)
							{
								if (ignore.Condition == JsonIgnoreCondition.WhenWritingDefault && jvalue.IsNull)
								{
									continue;
								}
								else if (ignore.Condition == JsonIgnoreCondition.Always)
								{
									continue;
								}
							}

							object? jsonValueObj = jvalue.MaybeNull()?.Get(prop.PropertyType);
							prop.SetValue(objInstance, jsonValueObj);
						}
					}

					return objInstance;
				}
			}
		}

		return DeserializeObjectX(value, tinputType, 0, options);
	}

	public static JsonValue SerializeObject(object? value, int deepness, bool convertersEnabled, JsonOptions options, out JsonValueType valueType)
	{
		if (deepness > options.DynamicObjectMaxDepth)
		{
			throw new InvalidOperationException("The JSON serialization reached it's maximum depth.");
		}
		if (value is null)
		{
			valueType = JsonValueType.Null;
			return new JsonValue(valueType, 0, null, options);
		}

		var itemType = value.GetType();

		if (value is string || value is char)
		{
			valueType = JsonValueType.String;
			return new JsonValue(valueType, 0, value.ToString(), options);
		}
		else if (value is int nint)
		{
			valueType = JsonValueType.Number;
			return new JsonValue(valueType, nint, null, options);
		}
		else if (value is byte || value is sbyte || value is uint || value is long || value is ulong)
		{
			valueType = JsonValueType.Number;
			return new JsonValue(valueType, Convert.ToInt64(value), null, options);
		}
		else if (value is double ndbl)
		{
			valueType = JsonValueType.Number;
			return new JsonValue(valueType, ndbl, null, options);
		}
		else if (value is float || value is decimal)
		{
			valueType = JsonValueType.Number;
			return new JsonValue(valueType, Convert.ToDouble(value), null, options);
		}
		else if (value is bool nbool)
		{
			valueType = JsonValueType.Boolean;
			return new JsonValue(valueType, nbool ? 1 : 0, null, options);
		}

		if (convertersEnabled)
		{
			for (int i = 0; i < options.Converters.Count; i++)
			{
				Converters.JsonConverter? mapper = options.Converters[i];
				if (mapper.CanSerialize(itemType))
				{
					var result = mapper.Serialize(value);
					valueType = result.Type;
					return result;
				}
			}
		}

		if (!options.DynamicSerialization.HasFlag(DynamicSerializationMode.Write))
		{
			throw new InvalidOperationException($"No converter matched the object type {itemType.FullName}.");
		}

		if (itemType.IsAssignableTo(typeof(IEnumerable)))
		{
			JsonArray arr = new JsonArray(options);
			foreach (object? item in (IEnumerable)value)
			{
				if (item == null) continue;
				arr.Add(SerializeObject(item, deepness + 1, convertersEnabled, options, out _));
			}

			valueType = JsonValueType.Array;
			return new JsonValue(valueType, 0, arr, options);
		}
		else
		{
			JsonObject newObj = new JsonObject(options);
			PropertyInfo[] valueProperties = itemType
				.GetProperties(BindingFlags.Public | BindingFlags.Instance);

			for (int i = 0; i < valueProperties.Length; i++)
			{
				PropertyInfo property = valueProperties[i];
				var atrJsonIgnore = property.GetCustomAttribute<JsonIgnoreAttribute>();
				var atrJsonProperty = property.GetCustomAttribute<JsonPropertyNameAttribute>();

				if (atrJsonIgnore?.Condition == JsonIgnoreCondition.Always)
					continue;

				string name = atrJsonProperty?.Name ?? property.Name;
				object? v = property.GetValue(value);

				if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingNull && v is null)
					continue;
				if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingDefault && v == default)
					continue;

				JsonValue jsonValue = SerializeObject(v, deepness + 1, convertersEnabled, options, out _);
				newObj.Add(name, jsonValue);
			}

			if (options.SerializeFields)
			{
				FieldInfo[] fields = itemType
					.GetFields(BindingFlags.Public | BindingFlags.Instance);

				for (int i = 0; i < fields.Length; i++)
				{
					FieldInfo field = fields[i];
					var atrJsonIgnore = field.GetCustomAttribute<JsonIgnoreAttribute>();
					var atrJsonProperty = field.GetCustomAttribute<JsonPropertyNameAttribute>();

					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.Always)
						continue;

					string name = atrJsonProperty?.Name ?? field.Name;
					object? v = field.GetValue(value);

					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingNull && v is null)
						continue;
					if (atrJsonIgnore?.Condition == JsonIgnoreCondition.WhenWritingDefault && v == default)
						continue;

					JsonValue jsonValue = SerializeObject(v, deepness + 1, convertersEnabled, options, out _);
					newObj.Add(name, jsonValue);
				}
			}

			valueType = JsonValueType.Object;
			return new JsonValue(JsonValueType.Object, 0, newObj, options);
		}
	}
}
