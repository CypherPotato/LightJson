using LightJson.Schema;
using System;

namespace LightJson.Converters;

/// <summary>
/// Converts byte-arrays to and from JSON.
/// </summary>
public sealed class ByteArrayConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets a value indicating whether byte arrays should be encoded as JSON arrays
	/// instead of base‑64 strings.
	/// </summary>
	public static bool EncodeAsArrays { get; set; } = false;

	/// <inheritdoc/>
	public override bool CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(byte[]);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		if (value.IsString)
		{
			return Convert.FromBase64String(value.GetString());
		}
		else if (value.IsJsonArray)
		{
			return value.GetJsonArray().ToArray<byte>();
		}
		else
		{
			throw new ArgumentException($"Expected value to be a base-64 encoded string or array of bytes.");
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		if (EncodeAsArrays)
		{
			return new JsonArray(currentOptions, ((byte[])value));
		}
		else
		{
			return new JsonValue(Convert.ToBase64String((byte[])value));
		}
	}

	/// <inheritdoc/>
	public override JsonSchema GetSchema(JsonOptions options)
	{
		if (EncodeAsArrays)
		{
			return JsonSchema.CreateArraySchema(
				JsonSchema.CreateNumberSchema(minimum: 0, maximum: 255),
				description: "Array of bytes (0-255)");
		}
		else
		{
			return JsonSchema.CreateStringSchema(description: "Base64 encoded byte array");
		}
	}
}
