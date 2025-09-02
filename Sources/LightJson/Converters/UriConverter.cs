using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an <see cref="JsonConverter"/> which can serialize and deserialize <see cref="Uri"/> values.
/// </summary>
public sealed class UriConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(Uri);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return new Uri(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		var str = ((Uri)value).ToString();
		return new JsonValue(str, currentOptions);
	}
}