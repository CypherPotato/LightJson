using LightJson.Schema;
using System;
using System.Collections.Generic;

namespace LightJson.Converters;

/// <summary>
/// Represents an Json Converter which can serialize and deserialize <see cref="Exception"/> values.
/// </summary>
public sealed class ExceptionConverter : JsonConverter
{
	/// <inheritdoc/>
	public override bool CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type.IsAssignableTo(typeof(Exception));
	}

	/// <inheritdoc/>
	public override object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		string message = value["message"].GetString();
		return new Exception(message);
	}

	/// <inheritdoc/>
	public override JsonValue Serialize(object value, JsonOptions currentOptions)
	{
		var ex = (Exception)value;

		return new JsonObject()
		{
			["Type"] = value.GetType().Name,
			["Message"] = ex.Message
		};
	}

	/// <inheritdoc/>
	public override JsonSchema GetSchema(JsonOptions options)
	{
		return JsonSchema.CreateObjectSchema(
			new Dictionary<string, JsonSchema>
			{
				["Type"] = JsonSchema.CreateStringSchema(description: "Exception type name"),
				["Message"] = JsonSchema.CreateStringSchema(description: "Exception message")
			},
			description: "Exception object");
	}
}
