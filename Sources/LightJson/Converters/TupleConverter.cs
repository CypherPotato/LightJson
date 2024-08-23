using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace LightJson.Converters;

internal class TupleConverter : JsonConverter
{
	public override bool CanSerialize(Type type)
	{
		return type.IsAssignableTo(typeof(ITuple));
	}

	public override object Deserialize(JsonValue value, Type requestedType)
	{
		var arr = value.GetJsonArray();
		var gtypes = requestedType.GenericTypeArguments;

		object?[] values = new object?[gtypes.Length];

		for (int i = 0; i < gtypes.Length; i++)
		{
			values[i] = arr[i].Get(gtypes[i]);
		}

		var createMethod = typeof(ValueTuple)
			.GetMethods()
			.FirstOrDefault(m => m.GetGenericArguments().Length == gtypes.Length)?
			.MakeGenericMethod(gtypes);

		if (createMethod is null)
		{
			throw new MethodAccessException($"Couldn't find an static Create method for the tuple type {requestedType.FullName}.");
		}

		return createMethod.Invoke(null, values)!;
	}

	public override JsonValue Serialize(object value)
	{
		JsonArray jarr = new JsonArray();
		var tuple = (ITuple)value;

		for (int i = 0; i < tuple.Length; i++)
		{
			object? obj = tuple[i];
			jarr.Add(JsonValue.Serialize(obj));
		}

		return jarr;
	}
}
