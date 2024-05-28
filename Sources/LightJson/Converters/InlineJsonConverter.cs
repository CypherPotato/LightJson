using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

public class InlineJsonConverter<T> : JsonConverter
{
	public Func<JsonValue, T>? DeserializerCallback { get; set; }
	public Func<T, JsonValue>? SerializerCallback { get; set; }

	public InlineJsonConverter(Func<JsonValue, T>? deserializerCallback, Func<T, JsonValue>? serializerCallback)
	{
		DeserializerCallback = deserializerCallback;
		SerializerCallback = serializerCallback;
	}

	public override bool CanSerialize(Type type)
	{
		return type == typeof(T);
	}

	public override object? Deserialize(JsonValue value, Type requestedType)
	{
		if (DeserializerCallback is not null)
		{
			return DeserializerCallback(value);
		}
		else
		{
			throw new NotImplementedException("The deserializer of this JsonConverter is not implemented.");
		}
	}

	public override JsonValue Serialize(object value)
	{
		if (SerializerCallback is not null)
		{
			return SerializerCallback((T)value);
		}
		else
		{
			throw new NotImplementedException("The serializer of this JsonConverter is not implemented.");
		}
	}
}
