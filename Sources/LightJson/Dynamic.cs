using System;
using System.Collections;
using System.Reflection;
using System.Text.Json.Serialization;

namespace LightJson;

internal class Dynamic
{
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
			foreach (var mapper in options.Converters)
			{
				if (mapper.CanSerialize(itemType))
				{
					var result = mapper.Serialize(value);
					valueType = result.Type;
					return result;
				}
			}
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

			foreach (PropertyInfo property in valueProperties)
			{
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

				foreach (FieldInfo field in fields)
				{
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
