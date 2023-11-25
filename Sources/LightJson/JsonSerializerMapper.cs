using System;
using System.Collections.Generic;
using System.Text;

namespace LightJson;

public abstract class JsonSerializerMapper
{
	public abstract bool CanSerialize(Type obj);
	public abstract JsonValue Serialize(object value);
	public abstract object Deserialize(JsonValue value);
}
