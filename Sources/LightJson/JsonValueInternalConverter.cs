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
