using LightJson.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LightJson.Serialization;

static class JsonDeserializer
{
	public static object Deserialize(JsonValue value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] Type objectType, int deepness, bool enableConverters, JsonOptions options)
	{
		if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			// map nullable struct to their underlaying type
			objectType = Nullable.GetUnderlyingType(objectType)!;
		}

		if (deepness > options.DynamicObjectMaxDepth)
		{
			throw new JsonException("The JSON deserialization reached it's maximum depth.");
		}
		else if (objectType == typeof(JsonValue))
		{
			return value;
		}
		else if (objectType == typeof(JsonObject))
		{
			return value.GetJsonObject();
		}
		else if (objectType == typeof(JsonArray))
		{
			return value.GetJsonArray();
		}
		else if (objectType == typeof(int) || objectType == typeof(uint) ||
				   objectType == typeof(long) || objectType == typeof(ulong) ||
				   objectType == typeof(double) || objectType == typeof(float) ||
				   objectType == typeof(byte) || objectType == typeof(sbyte) ||
				   objectType == typeof(decimal))
		{
			return Convert.ChangeType(value.GetNumber(), objectType);
		}
		else if (objectType == typeof(string))
		{
			return value.GetString();
		}
		else if (objectType == typeof(bool))
		{
			return value.GetBoolean();
		}
		else
		{
			// find the IJsonSerizeable interface on type
			if (JsonSerializableHelpers.TryDynamicDeserialize(value, options, objectType, out var result))
			{
				return result!;
			}

			// find a JsonConverter to match the specified type
			IList<JsonConverter> converters =
				enableConverters ?
				[.. options.Converters, .. JsonOptions.RequiredConverters] :
				JsonOptions.RequiredConverters;

			for (int i = 0; i < converters.Count; i++)
			{
				JsonConverter mapper = converters[i];
				if (mapper.CanSerialize(objectType, options))
				{
					try
					{
						return mapper.Deserialize(value, objectType, options);
					}
					catch (Exception ex)
					{
						throw new JsonException($"Unhandled exception while trying to convert {value.path} to {objectType.Name} through {mapper.GetType().Name}: {ex.Message}", ex);
					}
				}
			}

			if (options.SerializerContext is { } jcontext &&
				TryDeserializeWithTypeInfo(value, objectType, deepness, jcontext.TypeResolver, jcontext.SerializerOptions, enableConverters, options, out var deserializeResult))
			{
				return deserializeResult;
			}

			throw new JsonException($"Unable to deserialize the JSON value at {value.Path}: no converter matched the specified type.");
		}
	}

	static bool TryDeserializeWithTypeInfo(JsonValue value, Type objectType, int deepness, IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions, bool enableConverters, JsonOptions lightJsonOptions, [NotNullWhen(true)] out object? result)
	{
		var typeInfo = typeResolver.GetTypeInfo(objectType, serializerOptions);
		if (typeInfo == null)
		{
			result = null;
			return false;
		}

		result = Deserialize(value, typeInfo, deepness, enableConverters, lightJsonOptions, typeResolver, serializerOptions);
		return true;
	}

	private static object Deserialize(JsonValue value, JsonTypeInfo typeInfo, int deepness, bool enableConverters, JsonOptions lightJsonOptions, IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions)
	{
		Type objectType = typeInfo.Type;

		if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			objectType = Nullable.GetUnderlyingType(objectType)!;
			var underlyingTypeInfo = typeResolver.GetTypeInfo(objectType, serializerOptions);
			if (underlyingTypeInfo != null)
			{
				return Deserialize(value, underlyingTypeInfo, deepness, enableConverters, lightJsonOptions, typeResolver, serializerOptions);
			}
		}

		if (deepness > lightJsonOptions.DynamicObjectMaxDepth)
		{
			throw new JsonException("The JSON deserialization reached it's maximum depth.");
		}
		else if (objectType == typeof(JsonValue))
		{
			return value;
		}
		else if (objectType == typeof(JsonObject))
		{
			return value.GetJsonObject();
		}
		else if (objectType == typeof(JsonArray))
		{
			return value.GetJsonArray();
		}
		else if (objectType == typeof(int) || objectType == typeof(uint) ||
				   objectType == typeof(long) || objectType == typeof(ulong) ||
				   objectType == typeof(double) || objectType == typeof(float) ||
				   objectType == typeof(byte) || objectType == typeof(sbyte) ||
				   objectType == typeof(decimal))
		{
			return Convert.ChangeType(value.GetNumber(), objectType);
		}
		else if (objectType == typeof(string))
		{
			return value.GetString();
		}
		else if (objectType == typeof(bool))
		{
			return value.GetBoolean();
		}

		// find a JsonConverter to match the specified type
		IList<JsonConverter> converters =
			enableConverters ?
			[.. lightJsonOptions.Converters, .. JsonOptions.RequiredConverters] :
			JsonOptions.RequiredConverters;

		for (int i = 0; i < converters.Count; i++)
		{
			JsonConverter mapper = converters[i];
			if (mapper.CanSerialize(objectType, lightJsonOptions))
			{
				try
				{
					return mapper.Deserialize(value, objectType, lightJsonOptions);
				}
				catch (Exception ex)
				{
					throw new JsonException($"Unhandled exception while trying to convert {value.path} to {objectType.Name} through {mapper.GetType().Name}: {ex.Message}", ex);
				}
			}
		}

#pragma warning disable IL2072
		// IJsonSerializable is a dynamic feature. In AOT, this might fail if the type is not preserved.
		if (JsonSerializableHelpers.TryDynamicDeserialize(value, lightJsonOptions, objectType, out var customResult))
		{
			return customResult!;
		}
#pragma warning restore IL2072

		switch (typeInfo.Kind)
		{
			case JsonTypeInfoKind.Object:

				{
					HashSet<string> shouldIgnoreProperties = new HashSet<string>(JsonSanitizedComparer.Instance);
					var jobj = value.GetJsonObject();

					object createdEntity;
					if (typeInfo.CreateObject is { })
					{
						createdEntity = typeInfo.CreateObject();
					}
					else if (typeInfo.ConstructorAttributeProvider is ConstructorInfo constructorInfo)
					{
						var parameters = constructorInfo.GetParameters();
						object?[] parametersObjectList = new object?[parameters.Length];

						for (int i = 0; i < parameters.Length; i++)
						{
							var param = parameters[i];
							if (param.Name is null)
								continue;

							JsonValue matchedValue = jobj.GetValue(param.Name, JsonSanitizedComparer.Instance);
							if (matchedValue.IsNull)
							{
								if (param.HasDefaultValue)
								{
									parametersObjectList[i] = param.DefaultValue;
								}
								else
								{
									throw new JsonException($"The property '{value.Path}.{param.Name}' is required.");
								}
							}
							else
							{
								var paramTypeInfo = typeResolver.GetTypeInfo(param.ParameterType, serializerOptions)
									?? throw new JsonException($"Type {param.ParameterType} is not supported in AOT context.");
								parametersObjectList[i] = Deserialize(matchedValue, paramTypeInfo, deepness + 1, enableConverters, lightJsonOptions, typeResolver, serializerOptions);
							}

							_ = shouldIgnoreProperties.Add(param.Name);
						}

						createdEntity = constructorInfo.Invoke(parametersObjectList);
					}
					else
					{
						throw new JsonException($"Couldn't create an instance of the {objectType.Name} type.");
					}

					foreach (var property in typeInfo.Properties)
					{
						if (property.Set is null || shouldIgnoreProperties.Contains(property.Name))
						{
							continue;
						}

						string jsonKey = property.Name;
						if (property.AttributeProvider?.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false) is object[] { Length: > 0 } attrs)
						{
							jsonKey = ((System.Text.Json.Serialization.JsonPropertyNameAttribute)attrs[0]).Name;
						}
						else if (lightJsonOptions.NamingPolicy != null)
						{
							jsonKey = lightJsonOptions.NamingPolicy.ConvertName(property.Name);
						}

						object? propertyValue;
						JsonValue matchedProperty = jobj[jsonKey];
						if (matchedProperty.Type == JsonValueType.Undefined)
						{
							if (property.IsRequired)
							{
								throw new JsonException($"The property '{matchedProperty.Path}' is required.");
							}
							continue;
						}
						else if (matchedProperty.IsNull && property.IsSetNullable)
						{
							propertyValue = null;
						}
						else
						{
							var propTypeInfo = typeResolver.GetTypeInfo(property.PropertyType, serializerOptions)
								?? throw new JsonException($"Type {property.PropertyType} is not supported in AOT context.");
							propertyValue = Deserialize(matchedProperty, propTypeInfo, deepness + 1, enableConverters, lightJsonOptions, typeResolver, serializerOptions);
						}

						property.Set(createdEntity, propertyValue);
					}

					return createdEntity;
				}

			case JsonTypeInfoKind.Enumerable:
				{
					ArrayList array = [];

					var elementType = typeInfo.ElementType!;
					var elementTypeInfo = typeResolver.GetTypeInfo(elementType, serializerOptions)
						?? throw new JsonException($"Type {elementType} is not supported in AOT context.");

					foreach (var item in value.GetJsonArray())
					{
						object? arrayItem = Deserialize(item, elementTypeInfo, deepness + 1, enableConverters, lightJsonOptions, typeResolver, serializerOptions);
						_ = array.Add(arrayItem);
					}

					var genTypeDefinition = typeInfo.Type.IsGenericType switch
					{
						true => typeInfo.Type.GetGenericTypeDefinition(),
						false => typeInfo.Type
					};
					if (typeInfo.CreateObject is { } factory)
					{
						var list = factory();
						foreach (var item in array)
						{
							((IList)list).Add(item);
						}
						return list;
					}
					else if (genTypeDefinition == typeof(ArrayList))
					{
						return array;
					}
					else if (genTypeDefinition.IsArray)
					{
						// Array.CreateInstance is marked as RequiresDynamicCode, but for arrays of types
						// that are already known/preserved (which is implied by having JsonTypeInfo),
						// this should generally work or is the only way to create an array of a runtime Type.
#pragma warning disable IL3050
						var resultArr = Array.CreateInstance(elementType, array.Count);
#pragma warning restore IL3050
						array.CopyTo(resultArr);
						return resultArr;
					}
					else
					{
						throw new JsonException($"The collection type '{typeInfo.Type.Name}' is not supported. Use a array, HashSet<>, List<> or IList implementation instead.");
					}
				}

			case JsonTypeInfoKind.Dictionary:
				{
					var dict = value.GetJsonObject().Properties;
					object createdEntity;
					if (typeInfo.CreateObject is { })
					{
						createdEntity = typeInfo.CreateObject();
					}
					else
					{
						throw new JsonException($"Couldn't create an instance of the {objectType.Name} type.");
					}

					Type keyType = typeof(string);
					if (objectType.IsGenericType)
					{
						var args = objectType.GetGenericArguments();
						if (args.Length == 2)
						{
							keyType = args[0];
						}
					}

					var valueTypeInfo = typeResolver.GetTypeInfo(typeInfo.ElementType!, serializerOptions)
						?? throw new JsonException($"Type {typeInfo.ElementType} is not supported in AOT context.");

					foreach (var item in dict)
					{
						object? valueItem = Deserialize(item.Value, valueTypeInfo, deepness + 1, enableConverters, lightJsonOptions, typeResolver, serializerOptions);

						object key = item.Key;
						if (keyType != typeof(string))
						{
							key = Convert.ChangeType(item.Key, keyType);
						}

						((IDictionary)createdEntity).Add(key, valueItem);
					}

					return createdEntity;
				}
		}

		throw new JsonException($"Unable to deserialize the JSON value at {value.Path}: no converter matched the specified type.");
	}
}
