using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize enums.
/// </summary>
public sealed class EnumConverter : JsonConverter
{
	record EnumFieldInfo(string FieldName, object Value);

	/// <summary>
	/// Gets or sets whether this converter should serialize enum values into their
	/// string representation or numeric ones.
	/// </summary>
	public static bool EnumToString { get; set; } = false;

	/// <summary>
	/// Gets or sets whether this converter should serialize enum flags into arrays.
	/// </summary>
	public static bool SerializeFlagsIntoArrays { get; set; } = false;

	private static Dictionary<Type, EnumFieldInfo[]> _enumCache = [];

	[SuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.",
		Justification = "We just hope enumType.GetFields() bring all required enum fields")]
	static object GetEnumValue(Type enumType, string name, JsonOptions options)
	{
		if (_enumCache.TryGetValue(enumType, out var cache))
		{
			if (cache.FirstOrDefault(m => m.FieldName.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } foundField)
			{
				return foundField.Value;
			}
			throw new ArgumentOutOfRangeException($"The enum value '{name}' is not valid for the enum type '{enumType.Name}'.");
		}
		else
		{
			List<EnumFieldInfo> buildingCache = [];
			foreach (FieldInfo field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				string fieldRealName = field.Name;
				string fieldJsonName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? fieldRealName;

				object fieldValue = field.GetValue(null)!;
				buildingCache.Add(new EnumFieldInfo(fieldJsonName, fieldValue));
			}
			_enumCache[enumType] = buildingCache.ToArray();

			return GetEnumValue(enumType, name, options);
		}
	}

	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type.IsEnum;
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		if (value.IsInteger)
		{
			return Enum.ToObject(requestedType, value.GetInteger());
		}
		else if (value.IsJsonArray)
		{
			int result = 0;
			foreach (var item in value.GetJsonArray())
			{
				result |= (int)GetEnumValue(requestedType, item.GetString(), currentOptions);
			}
			return Enum.ToObject(requestedType, result);
		}
		else
		{
			int result = 0;
			foreach (var part in value.GetString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
			{
				result |= (int)GetEnumValue(requestedType, part, currentOptions);
			}
			return Enum.ToObject(requestedType, result);
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		if (EnumToString)
		{
			if (SerializeFlagsIntoArrays)
			{
				return new JsonArray(currentOptions, [.. value.ToString()!.Split(",", StringSplitOptions.TrimEntries)]);
			}
			else
			{
				return new JsonValue(value.ToString()!);
			}
		}
		else
		{
			return new JsonValue((int)value);
		}
	}
}

