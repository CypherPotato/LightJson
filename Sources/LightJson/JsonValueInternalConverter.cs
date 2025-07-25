using LightJson.Schema;
using System;
using System.Text.Json;

namespace LightJson;

/// <summary>
/// Provides a custom JSON converter for <see cref="JsonValue"/>.
/// </summary>
public sealed class JsonInternalConverter : System.Text.Json.Serialization.JsonConverter<JsonValue>
{
	/// <inheritdoc/>
	public override JsonValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (var jsonDocument = JsonDocument.ParseValue(ref reader))
		{
			var jsonText = jsonDocument.RootElement.GetRawText();
			return JsonOptions.Default.Deserialize(jsonText);
		}
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, JsonValue value, JsonSerializerOptions options)
	{
		string json = JsonOptions.Default.SerializeJson(value);
		writer.WriteRawValue(json);
	}
}

/// <summary>
/// Provides a custom JSON converter for <see cref="JsonArray"/>.
/// </summary>
public sealed class JsonArrayInternalConverter : System.Text.Json.Serialization.JsonConverter<JsonArray>
{
	/// <inheritdoc/>
	public override JsonArray Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (var jsonDocument = JsonDocument.ParseValue(ref reader))
		{
			var jsonText = jsonDocument.RootElement.GetRawText();
			return JsonOptions.Default.Deserialize(jsonText).GetJsonArray();
		}
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, JsonArray value, JsonSerializerOptions options)
	{
		string json = JsonOptions.Default.SerializeJson(value);
		writer.WriteRawValue(json);
	}
}

/// <summary>
/// Provides a custom JSON converter for <see cref="JsonObject"/>.
/// </summary>
public sealed class JsonObjectInternalConverter : System.Text.Json.Serialization.JsonConverter<JsonObject>
{
	/// <inheritdoc/>
	public override JsonObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (var jsonDocument = JsonDocument.ParseValue(ref reader))
		{
			var jsonText = jsonDocument.RootElement.GetRawText();
			return JsonOptions.Default.Deserialize(jsonText).GetJsonObject();
		}
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, JsonObject value, JsonSerializerOptions options)
	{
		string json = JsonOptions.Default.SerializeJson(value);
		writer.WriteRawValue(json);
	}
}


/// <summary>
/// Provides a custom JSON converter for <see cref="JsonObject"/>.
/// </summary>
public sealed class JsonSchemaInternalConverter : System.Text.Json.Serialization.JsonConverter<JsonSchema>
{
	/// <inheritdoc/>
	public override JsonSchema Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using (var jsonDocument = JsonDocument.ParseValue(ref reader))
		{
			var jsonText = jsonDocument.RootElement.GetRawText();
			return JsonOptions.Default.Deserialize(jsonText).Get<JsonSchema>();
		}
	}

	/// <inheritdoc/>
	public override void Write(Utf8JsonWriter writer, JsonSchema value, JsonSerializerOptions options)
	{
		string json = JsonOptions.Default.SerializeJson(value);
		writer.WriteRawValue(json);
	}
}
