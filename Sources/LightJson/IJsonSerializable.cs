using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace LightJson;

class DummySerializable : IJsonSerializable<DummySerializable>
{
	public static JsonValue SerializeIntoJson(DummySerializable self, JsonOptions options) => throw new NotImplementedException();
	public static DummySerializable DeserializeFromJson(JsonValue json, JsonOptions options) => throw new NotImplementedException();
}

/// <summary>
/// Defines a mechanism for serializing and deserializing a JSON value into a value.
/// </summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface IJsonSerializable<TSelf> where TSelf : IJsonSerializable<TSelf>?
{
	/// <summary>
	/// Serializes a <typeparamref name="TSelf"/> into an <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="self">The <typeparamref name="TSelf"/> object which will be serialized.</param>
	/// <param name="options">The contextual <see cref="JsonOptions"/> for the serialization.</param>
	public static abstract JsonValue SerializeIntoJson(TSelf self, JsonOptions options);

	/// <summary>
	/// Deserializes the specified <see cref="JsonValue"/> into an <typeparamref name="TSelf"/>.
	/// </summary>
	/// <param name="json">The provided JSON encoded value.</param>
	/// <param name="options">The contextual <see cref="JsonOptions"/> for the serialization.</param>
	public static abstract TSelf DeserializeFromJson(JsonValue json, JsonOptions options);
}

#pragma warning disable IL2070
internal static class JsonSerializableHelpers
{
	private static readonly ConcurrentDictionary<Type, MethodInfo?> s_serializeMethodCache = new();
	private static readonly ConcurrentDictionary<Type, MethodInfo?> s_deserializeMethodCache = new();
	private static readonly ConcurrentDictionary<Type, bool> s_interfaceImplementationCache = new();

	private const string SerializeMethodName = nameof(IJsonSerializable<DummySerializable>.SerializeIntoJson);
	private const string DeserializeMethodName = nameof(IJsonSerializable<DummySerializable>.DeserializeFromJson);
	private const BindingFlags PublicStaticFlags = BindingFlags.Public | BindingFlags.Static;

	private static bool ImplementsCorrectInterface(Type type)
	{
		if (s_interfaceImplementationCache.TryGetValue(type, out bool result))
		{
			return result;
		}

		bool implements = false;
		if (type.IsValueType || !type.IsAbstract)
		{
			foreach (var iface in type.GetInterfaces())
			{
				if (iface.IsGenericType &&
					iface.GetGenericTypeDefinition() == typeof(IJsonSerializable<>) &&
					iface.GetGenericArguments()[0] == type)
				{
					implements = true;
					break;
				}
			}
		}

		_ = s_interfaceImplementationCache.TryAdd(type, implements);
		return implements;
	}

	public static bool TryDynamicSerialize(object value, Type valueType, JsonOptions options, out JsonValue result)
	{
		if (value == null || value.GetType() != valueType)
		{
			result = JsonValue.Null;
			return false;
		}

		if (!ImplementsCorrectInterface(valueType))
		{
			result = JsonValue.Null;
			return false;
		}

		MethodInfo? serializeMethod = s_serializeMethodCache.GetOrAdd(valueType, type =>
		{
			return type.GetMethod(
				SerializeMethodName,
				PublicStaticFlags,
				null,
				new Type[] { type, typeof(JsonOptions) },
				null);
		});

		if (serializeMethod == null)
		{
			result = JsonValue.Null;
			return false;
		}

		try
		{
			result = (JsonValue)serializeMethod.Invoke(null, [value, options])!;
			return true;
		}
		catch (TargetInvocationException ex)
		{
			throw new JsonException($"Error during JSON serialization of {valueType.Name}: {ex.InnerException?.Message ?? ex.Message}", ex);
		}
		catch (Exception ex)
		{
			throw new JsonException($"Unexpected reflection/invocation error during serialization of {valueType.Name}: {ex.Message}");
		}
	}

	public static bool TryDynamicDeserialize(in JsonValue json, JsonOptions options, Type targetType, out object? result)
	{
		result = null;
		if (targetType == null)
		{
			return false;
		}

		if (!ImplementsCorrectInterface(targetType))
		{
			return false;
		}

		MethodInfo? deserializeMethod = s_deserializeMethodCache.GetOrAdd(targetType, type =>
		{
			return type.GetMethod(
				DeserializeMethodName,
				PublicStaticFlags,
				null,
				new Type[] { typeof(JsonValue), typeof(JsonOptions) },
				null);
		});

		if (deserializeMethod == null)
		{
			return false;
		}

		try
		{
			result = deserializeMethod.Invoke(null, new object[] { json, options });
			return true;
		}
		catch (TargetInvocationException ex)
		{
			throw new JsonException($"Error during deserialization to {targetType.Name}: {ex.InnerException?.Message ?? ex.Message}", ex);
		}
		catch (Exception ex)
		{
			throw new JsonException($"Unexpected reflection/invocation error during deserialization to {targetType.Name}: {ex.InnerException?.Message ?? ex.Message}", ex);
		}
	}
}