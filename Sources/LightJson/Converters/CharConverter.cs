using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="char"/> values.
/// </summary>
public class CharConverter : JsonConverter
{
	/// <inheritdoc/>
	public override bool CanSerialize(Type type)
	{
		return type == typeof(char);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		string s = value.GetString();
		if(s.Length != 1)
		{
			throw new ArgumentException($"The JSON value at {value.Path} expects an string with an 1-char length.");
		}

		return s[0];
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		return new JsonValue(value.ToString()!);
	}
}
