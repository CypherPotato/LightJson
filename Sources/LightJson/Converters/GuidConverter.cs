using LightJson.Schema;
using System;

namespace LightJson.Converters;

/// <summary>
/// Represents an <see cref="JsonConverter"/> which can serialize and deserialize <see cref="Guid"/> values.
/// </summary>
public sealed class GuidConverter : JsonConverter
{
	/// <inheritdoc/>
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(Guid);
	}

	/// <inheritdoc/>
	public override Object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return Guid.Parse(value.GetString());
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(Object value, JsonOptions currentOptions)
	{
		return new JsonValue(value.ToString()!, currentOptions);
	}

	/// <inheritdoc/>
	public override JsonSchema GetSchema(JsonOptions options)
	{
		return JsonSchema.CreateStringSchema(format: JsonSchemaStringFormat.Uuid, description: "UUID/GUID string");
	}
}
