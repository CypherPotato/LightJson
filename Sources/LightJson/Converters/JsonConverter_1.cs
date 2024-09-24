using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an generic implementation of <see cref="JsonConverter"/> which provides properties
/// to deserializing and serializing JSON values.
/// </summary>
/// <typeparam name="T">The type which the JSON serializer will deserialize and serialize.</typeparam>
public sealed class JsonConverter<T> : JsonConverter where T : notnull
{
	/// <summary>
	/// Gets or sets the deserializer function of the converter.
	/// </summary>
	public Func<JsonValue, T>? DeserializerCallback { get; set; }

	/// <summary>
	/// Gets or sets the serializer function of the converter.
	/// </summary>
	public Func<T, JsonValue>? SerializerCallback { get; set; }

	/// <summary>
	/// Creates an new <see cref="JsonConverter{T}"/> instance with given serialization functions.
	/// </summary>
	/// <param name="onDeserialize">Represents the deserializer function of the converter.</param>
	/// <param name="onSerialize">Represents the serializer function of the converter.</param>
	public JsonConverter(Func<JsonValue, T>? onDeserialize, Func<T, JsonValue>? onSerialize)
	{
		this.DeserializerCallback = onDeserialize;
		this.SerializerCallback = onSerialize;
	}

	/// <inheritdoc/>
	public override bool CanSerialize(Type type)
	{
		return type.IsAssignableTo(typeof(T));
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		if (this.DeserializerCallback is not null)
		{
			return this.DeserializerCallback(value);
		}
		else
		{
			throw new NotImplementedException("The deserializer of this JsonConverter is not implemented.");
		}
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		if (this.SerializerCallback is not null)
		{
			return this.SerializerCallback((T)value);
		}
		else
		{
			throw new NotImplementedException("The serializer of this JsonConverter is not implemented.");
		}
	}
}
