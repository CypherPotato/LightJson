using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an abstract class which provides methods for converting JSON values into
/// user-defined types.
/// </summary>
public abstract class JsonConverter
{
	/// <summary>
	/// Gets the current <see cref="JsonOptions"/> object.
	/// </summary>
	public JsonOptions CurrentOptions { get; internal set; } = null!;

	/// <summary>
	/// Determines whether the specified type can be converted or not.
	/// </summary>
	/// <param name="type">The object type which will be mapped.</param>
	/// <returns>An boolean indicating if the type can be mapped or not.</returns>
	public abstract bool CanSerialize(Type type);

	/// <summary>
	/// Serializes the object into an <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="value">The object which will be converted to an JsonValue.</param>
	/// <returns>The converted JsonValue.</returns>
	public abstract JsonValue Serialize(object value);

	/// <summary>
	/// Deserializes an <see cref="JsonValue"/> into an object of specified type.
	/// </summary>
	/// <param name="value">The JsonValue which will be converted to an object.</param>
	/// <param name="requestedType">The requested object type for deserializing.</param>
	/// <returns>The converted object.</returns>
	public abstract object Deserialize(JsonValue value, Type requestedType);
}

