using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace LightJson;

public static class DynamicDeserializer {

	public static object DeserializeWithTypeInfo ( JsonValue value, Type objectType, IJsonTypeInfoResolver typeResolver, JsonSerializerOptions serializerOptions ) {
		var typeInfo = typeResolver.GetTypeInfo ( objectType, serializerOptions );

		if (typeInfo is null) {
			throw new InvalidOperationException ( "typeInfo is null" );
		}

		switch (typeInfo.Kind) {
			case JsonTypeInfoKind.Object:
				if (typeInfo.CreateObject is null) {
					throw new InvalidOperationException ( "typeInfo.CreateObject is null" );
				}

				var jobj = value.GetJsonObject ();
				var obj = typeInfo.CreateObject ();
				foreach (var property in typeInfo.Properties) {
					if (property.Set is null) {
						throw new InvalidOperationException ( "property.Set is null" );
					}

					JsonValue matchedProperty = jobj [ property.Name ];
					if (property.IsRequired && matchedProperty.Type == JsonValueType.Undefined) {
						throw new JsonException ( $"The property '{matchedProperty.Path}' is required." );
					}
				}

				break;
		}

		return obj;
	}
}
