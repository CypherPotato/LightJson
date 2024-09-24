using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using LightJson.Serialization;

namespace LightJson;

internal class Dynamic
{
	public static object DeserializeObject(JsonValue value, Type tinputType, JsonOptions options)
	{
		if (value.IsNull)
			throw new InvalidOperationException($"Cannot deserialize the JSON value at {value.Path} into {tinputType.Name} because it is null or undefined.");

		return DeserializeObjectX(value, tinputType, 0, options);
	}

	[UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
	static object DeserializeObjectX(JsonValue value, Type type, int deepness, JsonOptions options)
	{
		if (type == typeof(JsonValue))
		{
			return value;
		}
		else if (type == typeof(int) || type == typeof(uint) ||
				 type == typeof(long) || type == typeof(ulong) ||
				 type == typeof(double) || type == typeof(float) ||
				 type == typeof(byte) || type == typeof(sbyte) ||
				 type == typeof(decimal))
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
						throw new InvalidOperationException($"Caught exception while trying to convert {value.path} to {type.Name}: {ex.Message}");
					}
				}
			}

			if (options.DynamicSerialization.HasFlag(DynamicSerializationMode.Read))
			{
				return DeserializeDynamicObject(value, type, deepness + 1, options);
			}
			else
			{
				throw new InvalidOperationException($"No converter matched the object type {type.FullName}.");
			}
		}
	}

	[RequiresDynamicCode("This method requires dynamic-code to deserialize into dynamic objects.")]
	[SuppressMessage("Trimming", "IL2067:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.", Justification = "<Pendente>")]
	[SuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.", Justification = "<Pendente>")]
	[SuppressMessage("Trimming", "IL2026:Using dynamic types might cause types or members to be removed by trimmer.", Justification = "<Pendente>")]
	static object DeserializeDynamicObject(JsonValue value, Type type, int deepness, JsonOptions options)
	{
		if (type.IsAssignableTo(typeof(IEnumerable)))
		{
			Type subType = type.IsArray
				? type.GetElementType()!
				: type.GetGenericArguments()[0];

			var arr = value.GetJsonArray();

			ArrayList items = new ArrayList();
			for (int i = 0; i < arr.Count; i++)
			{
				var arrItem = arr[i];
				var obj = DeserializeObjectX(arrItem, subType, deepness + 1, options);
				items.Add(obj);
			}

			if (type.IsGenericType && !type.IsArray)
			{
				var genericCollection = typeof(ICollection<>).MakeGenericType(subType);
				if (type.IsAssignableTo(genericCollection))
				{
					dynamic? listInstance = Activator.CreateInstance(type);
					foreach (dynamic? obj in items)
					{
						listInstance?.Add(obj);
					}
					return listInstance!;
				}
				else
				{
					throw new InvalidOperationException($"Unsupported collection type: {type}. The JSON deserializer can only deserialize JsonArrays into arrays or ICollection<> objects.");
				}
			}
			else if (type.IsArray)
			{
				return items.ToArray(subType);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported collection type: {type}. The JSON deserializer can only deserialize JsonArrays into arrays or ICollection<> objects.");
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
						.Where(p => options.PropertyNameComparer.Equals(propName, p.Key))
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
				else
				{
					if (prop.GetCustomAttribute<JsonRequiredAttribute>(true) is JsonRequiredAttribute jrequired)
					{
						throw new JsonSerializationException($"At value {jobj.path}.{prop.Name} it is expected to have a {prop.PropertyType}.",
							JsonSerializationException.ErrorType.Unknown);
					}
				}
			}

			return objInstance;
		}
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

		if (value is JsonValue jval)
		{
			valueType = jval.Type;
			return jval;
		}
		else if (value is string || value is char)
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
