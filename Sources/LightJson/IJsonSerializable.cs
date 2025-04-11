using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace LightJson;

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

internal static class JsonSerializableHelpers
{
	public static bool TryDynamicSerialize(object value, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type valueType, JsonOptions options, out JsonValue result)
	{
		MethodInfo? miSerializeIntoJson = valueType?
			.GetMethod("SerializeIntoJson", BindingFlags.Public | BindingFlags.Static, new Type[] { valueType, typeof(JsonOptions) });

		if (miSerializeIntoJson is null)
		{
			result = JsonValue.Null;
			return false;
		}

		result = (JsonValue)miSerializeIntoJson.Invoke(null, new object?[] { value, options })!;
		return true;
	}

	public static bool TryDynamicDeserialize(in JsonValue json, JsonOptions options, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type targetType, out object? result)
	{
		MethodInfo? miDeserializeFromJson = targetType?
			.GetMethod("DeserializeFromJson", BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(JsonValue), typeof(JsonOptions) });

		if (miDeserializeFromJson is null)
		{
			result = null;
			return false;
		}

		result = miDeserializeFromJson.Invoke(null, new object?[] { json, options })!;
		return true;
	}
}