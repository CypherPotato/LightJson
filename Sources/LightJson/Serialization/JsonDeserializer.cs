using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LightJson.Serialization;

static class JsonDeserializer
{
	public static object Deserialize(JsonValue value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.Interfaces)] Type objectType, int deepness, JsonOptions lightJsonOptions)
	{
		if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(Nullable<>))
		{
			// map nullable struct to their underlaying type
			objectType = Nullable.GetUnderlyingType(objectType)!;
		}

		if (objectType == typeof(JsonValue))
		{
			return value;
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
		else if (objectType == typeof(JsonObject))
		{
			return value.GetJsonObject();
		}
		else if (objectType == typeof(JsonArray))
		{
			return value.GetJsonArray();
		}
		else
		{
			// find the IJsonSerizeable interface on type
			if (JsonSerializableHelpers.TryDynamicDeserialize(value, lightJsonOptions, objectType, out var result))
			{
				return result!;
			}

			// find a JsonConverter to match the specified type
			for (int i = 0; i < lightJsonOptions.Converters.Count; i++)
			{
				var mapper = lightJsonOptions.Converters[i];
				if (mapper.CanSerialize(objectType, lightJsonOptions))
				{
					try
					{
						return mapper.Deserialize(value, objectType, lightJsonOptions);
					}
					catch (Exception ex)
					{
						throw new InvalidOperationException($"Caught exception while trying to convert {value.path} to {objectType.Name}: {ex.Message}");
					}
				}
			}

			if (lightJsonOptions.SerializerContext is { } jcontext)
			{
				return DeserializeWithTypeInfo(value, objectType, deepness, jcontext.TypeResolver, jcontext.SerializerOptions, lightJsonOptions);
			}

			throw new InvalidOperationException($"Unable to deserialize the JSON value at {value.Path}: no converter matched the specified type.");
		}
	}

#pragma warning disable IL2072, IL3050
	static object DeserializeWithTypeInfo(JsonValue value, Type objectType, int deepness, IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions, JsonOptions lightJsonOptions)
	{
		var typeInfo = typeResolver.GetTypeInfo(objectType, serializerOptions);

		if (typeInfo is null)
		{
			throw new InvalidOperationException("typeInfo is null");
		}

		switch (typeInfo.Kind)
		{
			case JsonTypeInfoKind.Object:

				HashSet<string> shouldIgnoreProperties = new HashSet<string>(lightJsonOptions.PropertyNameComparer);
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

						JsonValue matchedValue = jobj.GetValue(param.Name, lightJsonOptions.PropertyNameComparer);
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
							parametersObjectList[i] = Deserialize(matchedValue, param.ParameterType, deepness + 1, lightJsonOptions);
						}

						_ = shouldIgnoreProperties.Add(param.Name);
					}

					createdEntity = constructorInfo.Invoke(parametersObjectList);
				}
				else
				{
					throw new InvalidOperationException($"Couldn't create an instance of the {objectType.Name} type.");
				}

				foreach (var property in typeInfo.Properties)
				{
					if (property.Set is null || shouldIgnoreProperties.Contains(property.Name))
					{
						continue;
					}

					object? propertyValue;
					JsonValue matchedProperty = jobj[property.Name];
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
						propertyValue = Deserialize(matchedProperty, property.PropertyType, deepness + 1, lightJsonOptions);
					}

					property.Set(createdEntity, propertyValue);
				}

				return createdEntity;

			case JsonTypeInfoKind.Enumerable:
				ArrayList array = [];

				var elementType = typeInfo.ElementType!;
				foreach (var item in value.GetJsonArray())
				{
					object? arrayItem = Deserialize(item, elementType, deepness + 1, lightJsonOptions);
					_ = array.Add(arrayItem);
				}

				return array.ToArray(elementType);
		}

		Debug.Fail("It shouldn't get here...");
		return null!;
	}
}
