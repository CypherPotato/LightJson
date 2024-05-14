using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="char"/> values.
/// </summary>
public class CharConverter : JsonConverter
{
	/// <summary>
	/// Gets or sets the error message when this converter gets an invalid string.
	/// </summary>
	public static string ConvertErrorMessage { get; set; } = "The JSON value at {0} expects an string with an 1-char length.";

	/// <inheritdoc/>
	public override bool CanSerialize(Type type)
	{
		return type == typeof(char);
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType)
	{
		string s = value.GetString();
		if (s.Length != 1)
		{
			throw new ArgumentException(string.Format(ConvertErrorMessage, value.Path));
		}

		return s[0];
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value)
	{
		return new JsonValue(value.ToString()!);
	}
}
