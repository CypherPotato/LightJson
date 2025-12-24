using LightJson.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace LightJson.Schema;

/// <summary>
/// Provides methods for creating JSON schemas from types and delegates.
/// </summary>
static class JsonSchemaLoader
{
	/// <summary>
	/// Creates a JSON schema from the specified type.
	/// Uses JsonTypeInfo when available (AOT-safe), otherwise falls back to reflection.
	/// </summary>
	public static JsonSchema CreateFromTypeCore(Type t, JsonOptions options)
	{
		return CreateSchemaForType(t, options, new HashSet<Type>());
	}

	/// <summary>
	/// Creates a JSON schema from the signature of a delegate.
	/// This method always uses reflection and requires dynamic code support.
	/// </summary>
	public static JsonSchema CreateFromDelegateCore(Delegate d, JsonOptions options)
	{
		var method = d.Method;
		var parameters = method.GetParameters();

		var properties = new Dictionary<string, JsonSchema>();
		var requiredProperties = new List<string>();

		foreach (var param in parameters)
		{
			if (param.Name is null)
				continue;

			var paramSchema = CreateSchemaForType(param.ParameterType, options, new HashSet<Type>());

			string jsonName = options.NamingPolicy?.ConvertName(param.Name) ?? param.Name;
			properties[jsonName] = paramSchema;

			if (!param.HasDefaultValue && !IsNullableType(param.ParameterType))
			{
				requiredProperties.Add(jsonName);
			}
		}

		return JsonSchema.CreateObjectSchema(properties, requiredProperties.Count > 0 ? requiredProperties : null);
	}

	private static JsonSchema CreateSchemaForType(Type type, JsonOptions options, HashSet<Type> processingTypes)
	{
		// Handle circular references
		if (!processingTypes.Add(type))
		{
			return new JsonSchema(new JsonObject { ["type"] = "object", ["description"] = $"Circular reference to {type.Name}" });
		}

		try
		{
			return CreateSchemaForTypeCore(type, options, processingTypes);
		}
		finally
		{
			processingTypes.Remove(type);
		}
	}

	private static JsonSchema CreateSchemaForTypeCore(Type type, JsonOptions options, HashSet<Type> processingTypes)
	{
		// Handle nullable types
		var underlyingType = Nullable.GetUnderlyingType(type);
		if (underlyingType != null)
		{
			var innerSchema = CreateSchemaForType(underlyingType, options, processingTypes);
			return innerSchema.Nullable();
		}

		// Check if there's a custom converter for this type
		var converterSchema = TryGetSchemaFromConverter(type, options);
		if (converterSchema != null)
		{
			return converterSchema;
		}

		// Handle primitive and well-known types (no reflection needed)
		var primitiveSchema = TryCreatePrimitiveSchema(type);
		if (primitiveSchema != null)
		{
			return primitiveSchema;
		}

		// Handle arrays
		if (type.IsArray)
		{
			var elementType = type.GetElementType()!;
			var itemSchema = CreateSchemaForType(elementType, options, processingTypes);
			return JsonSchema.CreateArraySchema(itemSchema);
		}

		// Handle enums - requires reflection for proper schema generation with field names
		// This must be handled before the AOT path since JsonTypeInfo for enums has Kind=None
		if (type.IsEnum)
		{
			if (!RuntimeFeature.IsDynamicCodeSupported)
			{
				// In AOT mode without reflection, return a basic schema
				return EnumConverter.EnumToString
					? JsonSchema.CreateStringSchema()
					: JsonSchema.CreateNumberSchema(description: $"Enum value for {type.Name}");
			}

#pragma warning disable IL2067 // Target parameter does not have matching annotations
			return CreateEnumSchemaWithReflection(type, options);
#pragma warning restore IL2067
		}

		// Try AOT-safe path first: use JsonTypeInfo if available
		if (options.SerializerContext != null)
		{
			var typeInfo = options.SerializerContext.TypeResolver.GetTypeInfo(type, options.SerializerContext.SerializerOptions);
			if (typeInfo != null)
			{
				return CreateSchemaFromTypeInfo(typeInfo, options, processingTypes);
			}
		}

		// Reflection path: requires dynamic code support
		if (!RuntimeFeature.IsDynamicCodeSupported)
		{
			throw new InvalidOperationException(
				$"Cannot create schema for type '{type.FullName}' without a JsonSerializerContext. " +
				"In AOT/trimmed applications, provide a JsonOptionsSerializerContext via JsonOptions.SerializerContext " +
				"that includes this type.");
		}

		// The following code is only reachable when RuntimeFeature.IsDynamicCodeSupported is true,
		// which means reflection is available. The analyzer cannot verify this runtime guard,
		// so we suppress the warnings for these specific calls.
#pragma warning disable IL2067 // Target parameter does not have matching annotations
		return CreateSchemaFromReflection(type, options, processingTypes);
#pragma warning restore IL2067
	}

	private static JsonSchema? TryCreatePrimitiveSchema(Type type)
	{
		if (type == typeof(string))
			return JsonSchema.CreateStringSchema();

		if (type == typeof(bool))
			return JsonSchema.CreateBooleanSchema();

		if (IsNumericType(type))
			return CreateNumericSchema(type);

		if (type == typeof(JsonValue))
			return new JsonSchema(new JsonObject());

		if (type == typeof(JsonObject))
			return new JsonSchema(new JsonObject { ["type"] = "object" });

		if (type == typeof(JsonArray))
			return new JsonSchema(new JsonObject { ["type"] = "array" });

		return null;
	}

	private static JsonSchema? TryGetSchemaFromConverter(Type type, JsonOptions options)
	{
		foreach (var converter in options.Converters)
		{
			if (converter.CanSerialize(type, options))
			{
				// Try to get schema from converter
				// Converters that override GetSchema will return a schema
				// The base implementation returns null by default
				try
				{
					var schema = converter.GetSchema(options);
					if (schema != null)
					{
						return schema;
					}
				}
				catch
				{
					// If GetSchema throws, fall through to default behavior
				}

				return null;
			}
		}

		return null;
	}

	#region AOT-Safe Path (JsonTypeInfo)

	private static JsonSchema CreateSchemaFromTypeInfo(JsonTypeInfo typeInfo, JsonOptions options, HashSet<Type> processingTypes)
	{
		switch (typeInfo.Kind)
		{
			case JsonTypeInfoKind.Object:
				return CreateObjectSchemaFromTypeInfo(typeInfo, options, processingTypes);

			case JsonTypeInfoKind.Enumerable:
				if (typeInfo.ElementType != null)
				{
					var itemSchema = CreateSchemaForType(typeInfo.ElementType, options, processingTypes);
					return JsonSchema.CreateArraySchema(itemSchema);
				}
				return JsonSchema.CreateArraySchema();

			case JsonTypeInfoKind.Dictionary:
				if (typeInfo.ElementType != null)
				{
					var valueSchema = CreateSchemaForType(typeInfo.ElementType, options, processingTypes);
					return new JsonSchema(new JsonObject
					{
						["type"] = "object",
						["additionalProperties"] = valueSchema.AsJsonValue()
					});
				}
				return new JsonSchema(new JsonObject { ["type"] = "object" });

			default:
				return new JsonSchema(new JsonObject());
		}
	}

	private static JsonSchema CreateObjectSchemaFromTypeInfo(JsonTypeInfo typeInfo, JsonOptions options, HashSet<Type> processingTypes)
	{
		var properties = new Dictionary<string, JsonSchema>();
		var requiredProperties = new List<string>();

		foreach (var prop in typeInfo.Properties)
		{
			if (prop.Get is null && prop.Set is null)
				continue;

			if (prop.AttributeProvider != null)
			{
				var ignoreAttr = prop.AttributeProvider.GetCustomAttributes(typeof(JsonIgnoreAttribute), false)
					.Cast<JsonIgnoreAttribute>()
					.FirstOrDefault();

				if (ignoreAttr != null && ignoreAttr.Condition == JsonIgnoreCondition.Always)
					continue;
			}

			string jsonName = options.NamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
			var propSchema = CreateSchemaForType(prop.PropertyType, options, processingTypes);

			if (prop.IsSetNullable || IsNullableType(prop.PropertyType))
			{
				propSchema = propSchema.Nullable();
			}

			properties[jsonName] = propSchema;

			if (prop.IsRequired)
			{
				requiredProperties.Add(jsonName);
			}
		}

		return JsonSchema.CreateObjectSchema(properties, requiredProperties.Count > 0 ? requiredProperties : null);
	}

	#endregion

	#region Reflection Path (Dynamic Code)

	private const DynamicallyAccessedMemberTypes ReflectionMemberTypes =
		DynamicallyAccessedMemberTypes.PublicProperties |
		DynamicallyAccessedMemberTypes.PublicFields |
		DynamicallyAccessedMemberTypes.Interfaces;

	private static JsonSchema CreateSchemaFromReflection(
		[DynamicallyAccessedMembers(ReflectionMemberTypes)] Type type,
		JsonOptions options,
		HashSet<Type> processingTypes)
	{
		// Handle collections via reflection
		if (TryGetCollectionElementType(type, out var collectionElementType))
		{
			var itemSchema = CreateSchemaForType(collectionElementType, options, processingTypes);
			return JsonSchema.CreateArraySchema(itemSchema);
		}

		// Handle dictionaries via reflection
		if (TryGetDictionaryTypes(type, out var keyType, out var valueType))
		{
			if (keyType == typeof(string))
			{
				var valueSchema = CreateSchemaForType(valueType, options, processingTypes);
				return new JsonSchema(new JsonObject
				{
					["type"] = "object",
					["additionalProperties"] = valueSchema.AsJsonValue()
				});
			}
		}

		// Handle complex objects via reflection
		return CreateObjectSchemaFromReflection(type, options, processingTypes);
	}

	private static JsonSchema CreateObjectSchemaFromReflection(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type,
		JsonOptions options,
		HashSet<Type> processingTypes)
	{
		var properties = new Dictionary<string, JsonSchema>();
		var requiredProperties = new List<string>();

		// Process public properties
		foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
		{
			if (prop.GetIndexParameters().Length > 0)
				continue;

			var ignoreAttr = prop.GetCustomAttribute<JsonIgnoreAttribute>();
			if (ignoreAttr != null && ignoreAttr.Condition == JsonIgnoreCondition.Always)
				continue;

			string jsonName = GetJsonPropertyName(prop, options);
			var propSchema = CreateSchemaForType(prop.PropertyType, options, processingTypes);

			properties[jsonName] = propSchema;

			var requiredAttr = prop.GetCustomAttribute<JsonRequiredAttribute>();
			if (requiredAttr != null)
			{
				requiredProperties.Add(jsonName);
			}
		}

		// Process public fields
		foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
		{
			var ignoreAttr = field.GetCustomAttribute<JsonIgnoreAttribute>();
			if (ignoreAttr != null && ignoreAttr.Condition == JsonIgnoreCondition.Always)
				continue;

			string jsonName = GetJsonFieldName(field, options);
			var fieldSchema = CreateSchemaForType(field.FieldType, options, processingTypes);

			properties[jsonName] = fieldSchema;

			var requiredAttr = field.GetCustomAttribute<JsonRequiredAttribute>();
			if (requiredAttr != null)
			{
				requiredProperties.Add(jsonName);
			}
		}

		return JsonSchema.CreateObjectSchema(properties, requiredProperties.Count > 0 ? requiredProperties : null);
	}

	private static JsonSchema CreateEnumSchemaWithReflection(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] Type enumType,
		JsonOptions options)
	{
		if (EnumConverter.EnumToString)
		{
			var values = new List<string>();
			foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				var ignoreAttr = field.GetCustomAttribute<JsonIgnoreAttribute>();
				if (ignoreAttr != null)
					continue;

				var enumMemberName = field.GetCustomAttribute<JsonStringEnumMemberNameAttribute>();
				values.Add(enumMemberName?.Name ?? field.Name);
			}
			return JsonSchema.CreateStringSchema(enums: values);
		}
		else
		{
			return JsonSchema.CreateNumberSchema(description: $"Enum value for {enumType.Name}");
		}
	}

	private static bool TryGetCollectionElementType(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
		[NotNullWhen(true)] out Type? elementType)
	{
		// Check for generic IEnumerable<T>
		if (type.IsGenericType)
		{
			var genericDef = type.GetGenericTypeDefinition();
			if (genericDef == typeof(IEnumerable<>) ||
				genericDef == typeof(IList<>) ||
				genericDef == typeof(ICollection<>) ||
				genericDef == typeof(List<>) ||
				genericDef == typeof(IReadOnlyList<>) ||
				genericDef == typeof(IReadOnlyCollection<>))
			{
				elementType = type.GetGenericArguments()[0];
				return true;
			}
		}

		// Check interfaces for IEnumerable<T>
		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
			{
				elementType = iface.GetGenericArguments()[0];
				if (type != typeof(string))
					return true;
			}
		}

		// Check for non-generic IEnumerable
		if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string))
		{
			elementType = typeof(object);
			return true;
		}

		elementType = null;
		return false;
	}

	private static bool TryGetDictionaryTypes(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type,
		[NotNullWhen(true)] out Type? keyType,
		[NotNullWhen(true)] out Type? valueType)
	{
		// Check if type itself is a generic dictionary
		if (type.IsGenericType)
		{
			var genericDef = type.GetGenericTypeDefinition();
			if (genericDef == typeof(IDictionary<,>) ||
				genericDef == typeof(Dictionary<,>) ||
				genericDef == typeof(IReadOnlyDictionary<,>))
			{
				var args = type.GetGenericArguments();
				keyType = args[0];
				valueType = args[1];
				return true;
			}
		}

		// Check interfaces for IDictionary<K,V>
		foreach (var iface in type.GetInterfaces())
		{
			if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
			{
				var args = iface.GetGenericArguments();
				keyType = args[0];
				valueType = args[1];
				return true;
			}
		}

		keyType = null;
		valueType = null;
		return false;
	}

	private static string GetJsonPropertyName(PropertyInfo prop, JsonOptions options)
	{
		var nameAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
		if (nameAttr != null)
			return nameAttr.Name;

		return options.NamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
	}

	private static string GetJsonFieldName(FieldInfo field, JsonOptions options)
	{
		var nameAttr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
		if (nameAttr != null)
			return nameAttr.Name;

		return options.NamingPolicy?.ConvertName(field.Name) ?? field.Name;
	}

	#endregion

	#region Helpers

	private static JsonSchema CreateNumericSchema(Type type)
	{
		if (type == typeof(int) || type == typeof(uint) ||
			type == typeof(long) || type == typeof(ulong) ||
			type == typeof(short) || type == typeof(ushort) ||
			type == typeof(byte) || type == typeof(sbyte))
		{
			var schema = new JsonObject { ["type"] = "integer" };

			if (type == typeof(int))
			{
				schema["minimum"] = int.MinValue;
				schema["maximum"] = int.MaxValue;
			}
			else if (type == typeof(uint))
			{
				schema["minimum"] = uint.MinValue;
				schema["maximum"] = uint.MaxValue;
			}
			else if (type == typeof(long))
			{
				schema["minimum"] = long.MinValue;
				schema["maximum"] = long.MaxValue;
			}
			else if (type == typeof(byte))
			{
				schema["minimum"] = byte.MinValue;
				schema["maximum"] = byte.MaxValue;
			}
			else if (type == typeof(short))
			{
				schema["minimum"] = short.MinValue;
				schema["maximum"] = short.MaxValue;
			}

			return new JsonSchema(schema);
		}

		return JsonSchema.CreateNumberSchema();
	}

	private static bool IsNumericType(Type type)
	{
		return type == typeof(int) || type == typeof(uint) ||
			   type == typeof(long) || type == typeof(ulong) ||
			   type == typeof(short) || type == typeof(ushort) ||
			   type == typeof(byte) || type == typeof(sbyte) ||
			   type == typeof(float) || type == typeof(double) ||
			   type == typeof(decimal);
	}

	private static bool IsNullableType(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
	}

	#endregion
}
