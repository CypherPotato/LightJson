using LightJson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LightJson.Schema
{
	/// <summary>
	/// Represents a JSON Schema that can be used to validate JSON values.
	/// This implementation covers a core subset of the JSON Schema Draft 7 specification.
	/// </summary>
	[JsonConverter(typeof(JsonSchemaInternalConverter))]
	public sealed class JsonSchema : IJsonSerializable<JsonSchema>, IImplicitJsonValue
	{
		private readonly JsonObject schema;

		/// <summary>
		/// Creates a JSON schema for strings.
		/// </summary>
		/// <param name="minLength">The minimum length of the string. Can be <see langword="null"/>.</param>
		/// <param name="maxLength">The maximum length of the string. Can be <see langword="null"/>.</param>
		/// <param name="pattern">A regular expression that the string must match. Can be <see langword="null"/>.</param>
		/// <param name="format">The format of the string (e.g., "email", "date-time"). Can be <see langword="null"/>.</param>
		/// <param name="enums">A list of allowed string values. Can be <see langword="null"/>.</param>
		/// <param name="description">A description of the schema. Can be <see langword="null"/>.</param>
		/// <returns>A <see cref="JsonSchema"/> for strings.</returns>
		public static JsonSchema CreateStringSchema(int? minLength = null, int? maxLength = null, string? pattern = null, string? format = null, IEnumerable<string>? enums = null, string? description = null)
		{
			var schema = new JsonObject()
			{
				["type"] = "string"
			};
			if (minLength is { }) schema["minLength"] = minLength.Value;
			if (maxLength is { }) schema["maxLength"] = maxLength.Value;
			if (pattern is { }) schema["pattern"] = pattern;
			if (format is { }) schema["format"] = format;
			if (enums is { }) schema["enum"] = new JsonArray(enums);
			if (description is { }) schema["description"] = description;

			return new JsonSchema(schema);
		}

		/// <summary>
		/// Creates a JSON schema for numbers.
		/// </summary>
		/// <param name="minimum">The minimum value (inclusive). Can be <see langword="null"/>.</param>
		/// <param name="maximum">The maximum value (inclusive). Can be <see langword="null"/>.</param>
		/// <param name="exclusiveMinimum">The minimum value (exclusive). Can be <see langword="null"/>.</param>
		/// <param name="exclusiveMaximum">The maximum value (exclusive). Can be <see langword="null"/>.</param>
		/// <param name="multipleOf">The value must be a multiple of this number. Can be <see langword="null"/>.</param>
		/// <param name="description">A description of the schema. Can be <see langword="null"/>.</param>
		/// <returns>A <see cref="JsonSchema"/> for numbers.</returns>
		public static JsonSchema CreateNumberSchema(double? minimum = null, double? maximum = null, double? exclusiveMinimum = null, double? exclusiveMaximum = null, double? multipleOf = null, string? description = null)
		{
			var schema = new JsonObject
			{
				["type"] = "number"
			};
			if (minimum is { }) schema["minimum"] = minimum.Value;
			if (maximum is { }) schema["maximum"] = maximum.Value;
			if (exclusiveMinimum is { }) schema["exclusiveMinimum"] = exclusiveMinimum.Value;
			if (exclusiveMaximum is { }) schema["exclusiveMaximum"] = exclusiveMaximum.Value;
			if (multipleOf is { }) schema["multipleOf"] = multipleOf.Value;
			if (description is { }) schema["description"] = description;

			return new JsonSchema(schema);
		}

		/// <summary>
		/// Creates a JSON schema for objects.
		/// </summary>
		/// <param name="properties">The properties of the object, where the key is the property name and the value is the schema for the property. Can be <see langword="null"/>.</param>
		/// <param name="requiredProperties">A list of required property names. Can be <see langword="null"/>.</param>
		/// <param name="description">A description of the schema. Can be <see langword="null"/>.</param>
		/// <returns>A <see cref="JsonSchema"/> for objects.</returns>
		public static JsonSchema CreateObjectSchema(IDictionary<string, JsonSchema>? properties = null, IEnumerable<string>? requiredProperties = null, string? description = null)
		{
			var schema = new JsonObject
			{
				["type"] = "object"
			};
			if (properties is { })
			{
				var propertiesObject = new JsonObject();
				foreach (var kvp in properties)
				{
					propertiesObject[kvp.Key] = kvp.Value.schema;
				}

				schema["properties"] = propertiesObject;
			}
			if (requiredProperties is { }) schema["required"] = new JsonArray(requiredProperties);
			if (description is { }) schema["description"] = description;

			return new JsonSchema(schema);
		}

		/// <summary>
		/// Creates a JSON schema for arrays.
		/// </summary>
		/// <param name="itemsSchema">The schema for the items in the array. Can be <see langword="null"/>.</param>
		/// <param name="uniqueItems">Whether the items in the array must be unique. Can be <see langword="null"/>.</param>
		/// <param name="minItems">The minimum number of items in the array. Can be <see langword="null"/>.</param>
		/// <param name="maxItems">The maximum number of items in the array. Can be <see langword="null"/>.</param>
		/// <param name="description">A description of the schema. Can be <see langword="null"/>.</param>
		/// <returns>A <see cref="JsonSchema"/> for arrays.</returns>
		public static JsonSchema CreateArraySchema(JsonSchema? itemsSchema = null, bool? uniqueItems = null, int? minItems = null, int? maxItems = null, string? description = null)
		{
			var schema = new JsonObject
			{
				["type"] = "array"
			};
			if (itemsSchema is { }) schema["items"] = itemsSchema.schema;
			if (uniqueItems is { }) schema["uniqueItems"] = uniqueItems.Value;
			if (minItems is { }) schema["minItems"] = minItems.Value;
			if (maxItems is { }) schema["maxItems"] = maxItems.Value;
			if (description is { }) schema["description"] = description;

			return new JsonSchema(schema);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSchema"/> class with a schema definition.
		/// </summary>
		/// <param name="schema">The JsonObject representing the schema.</param>
		public JsonSchema(JsonObject schema)
		{
			this.schema = schema ?? throw new ArgumentNullException(nameof(schema));
		}

		/// <summary>
		/// Gets an indication of whether this schema is empty (i.e., has no properties).
		/// </summary>
		public bool IsEmpty => !this.schema.properties.Any();

		/// <summary>
		/// Validates a <see cref="JsonValue"/> against this schema.
		/// </summary>
		/// <param name="instance">The JSON value to validate.</param>
		/// <returns>A <see cref="JsonSchemaValidationResult"/> indicating success or failure.</returns>
		public JsonSchemaValidationResult Validate(JsonValue instance)
		{
			var errors = new List<JsonSchemaValidationError>();
			this.ValidateNode(instance, this.schema, "$", errors);
			return new JsonSchemaValidationResult(errors);
		}

		private void ValidateNode(JsonValue instance, JsonObject nodeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			if (nodeSchema.ContainsKey("type") && this.ValidateType(instance, nodeSchema["type"], path, errors))
			{
				switch (instance.Type)
				{
					case JsonValueType.Object:
						this.ValidateObject(instance.GetJsonObject(), nodeSchema, path, errors);
						break;
					case JsonValueType.Array:
						this.ValidateArray(instance.GetJsonArray(), nodeSchema, path, errors);
						break;
					case JsonValueType.String:
						this.ValidateString(instance.GetString(), nodeSchema, path, errors);
						break;
					case JsonValueType.Number:
						this.ValidateNumber(instance.GetNumber(), nodeSchema, path, errors);
						break;
				}
			}
		}

		private bool ValidateType(JsonValue instance, JsonValue typeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			JsonValueType MapJsonType(string type) => type switch
			{
				"object" => JsonValueType.Object,
				"number" => JsonValueType.Number,
				"integer" => JsonValueType.Number,
				"array" => JsonValueType.Array,
				"null" => JsonValueType.Null,
				"bool" => JsonValueType.Boolean,
				"boolean" => JsonValueType.Boolean,
				"string" => JsonValueType.String,
				_ => throw new ArgumentOutOfRangeException($"Unsupported or invalid JSON type: {type}")
			};

			JsonValueType[] expectedTypes = Array.Empty<JsonValueType>();
			if (typeSchema.IsString)
			{
				expectedTypes = [MapJsonType(typeSchema.GetString())];
			}
			else if (typeSchema.IsJsonArray)
			{
				expectedTypes = typeSchema.GetJsonArray().ToArray(n => MapJsonType(n.GetString()));
			}

			if (expectedTypes.Length > 0 && !expectedTypes.Contains(instance.Type))
			{
				errors.Add(new JsonSchemaValidationError(path, "type", $"Instance type '{instance.Type}' is not one of the expected types: {string.Join(", ", expectedTypes)}."));
				return false;
			}

			return true;
		}

		private void ValidateObject(JsonObject instance, JsonObject nodeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			// Validate required properties
			if (nodeSchema.ContainsKey("required") && nodeSchema["required"].IsJsonArray)
			{
				foreach (var requiredProp in nodeSchema["required"].GetJsonArray())
				{
					if (requiredProp.IsString && !instance.ContainsKey(requiredProp.GetString()))
					{
						errors.Add(new JsonSchemaValidationError(path, "required", $"Required property '{requiredProp.GetString()}' is missing."));
					}
				}
			}

			// Validate properties
			if (nodeSchema.ContainsKey("properties") && nodeSchema["properties"].IsJsonObject)
			{
				var propertiesSchema = nodeSchema["properties"].GetJsonObject();
				foreach (var property in instance)
				{
					if (propertiesSchema.ContainsKey(property.Key) && propertiesSchema[property.Key].IsJsonObject)
					{
						this.ValidateNode(property.Value, propertiesSchema[property.Key].GetJsonObject(), $"{path}.{property.Key}", errors);
					}
				}
			}
		}

		private void ValidateArray(JsonArray instance, JsonObject nodeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			if (nodeSchema.ContainsKey("minItems") && nodeSchema["minItems"].IsNumber && instance.Count < nodeSchema["minItems"].GetInteger())
			{
				errors.Add(new JsonSchemaValidationError(path, "minItems", $"Array must have at least {nodeSchema["minItems"].GetInteger()} items, but has {instance.Count}."));
			}

			if (nodeSchema.ContainsKey("maxItems") && nodeSchema["maxItems"].IsNumber && instance.Count > nodeSchema["maxItems"].GetInteger())
			{
				errors.Add(new JsonSchemaValidationError(path, "maxItems", $"Array must have at most {nodeSchema["maxItems"].GetInteger()} items, but has {instance.Count}."));
			}

			if (nodeSchema.ContainsKey("uniqueItems") && nodeSchema["uniqueItems"].IsBoolean && nodeSchema["uniqueItems"].GetBoolean())
			{
				var uniqueItems = new HashSet<JsonValue>();
				foreach (var item in instance)
				{
					if (!uniqueItems.Add(item))
					{
						errors.Add(new JsonSchemaValidationError(path, "uniqueItems", "Array must contain unique items. Duplicate found."));
						break;
					}
				}
			}

			if (nodeSchema.ContainsKey("items") && nodeSchema["items"].IsJsonObject)
			{
				var itemSchema = nodeSchema["items"].GetJsonObject();
				for (int i = 0; i < instance.Count; i++)
				{
					this.ValidateNode(instance[i], itemSchema, $"{path}[{i}]", errors);
				}
			}
		}

		private void ValidateString(string instance, JsonObject nodeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			if (nodeSchema["minLength"].TryGet<int>() is int { } minLength && instance.Length < minLength)
			{
				errors.Add(new JsonSchemaValidationError(path, "minLength", $"String must be at least {nodeSchema["minLength"].GetInteger()} characters long, but is {instance.Length}."));
			}
			if (nodeSchema["maxLength"].TryGet<int>() is int { } maxLength && instance.Length > maxLength)
			{
				errors.Add(new JsonSchemaValidationError(path, "maxLength", $"String must be at least {nodeSchema["maxLength"].GetInteger()} characters long, but is {instance.Length}."));
			}
			if (nodeSchema["pattern"].TryGet<string>() is string { } pattern)
			{
				if (!Regex.IsMatch(instance, pattern))
				{
					errors.Add(new JsonSchemaValidationError(path, "pattern", $"String does not match the required pattern: {pattern}."));
				}
			}
			if (nodeSchema["enum"].TryGet<JsonArray>() is JsonArray { } enumValues)
			{
				if (!enumValues.Any(e => e.IsString && e.GetString() == instance))
				{
					errors.Add(new JsonSchemaValidationError(path, "enum", $"String must be one of the enumerated values: [{string.Join(", ", enumValues.Select(e => e.GetString()))}], but is '{instance}'."));
				}
			}
			if (nodeSchema["format"].TryGet<string>() is { } format)
			{
				switch (format)
				{
					case "date-time":
						if (!DateTime.TryParse(instance, out _))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid date-time format."));
						}
						break;
					case "email":
						if (!Regex.IsMatch(instance, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid email format."));
						}
						break;
					case "time":
						if (!TimeOnly.TryParse(instance, out _))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid time format."));
						}
						break;
					case "duration":
						if (!TimeSpan.TryParse(instance, out _))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid ISO 8601 duration format."));
						}
						break;
					case "uri":
					case "url":
						if (!Uri.TryCreate(instance, UriKind.RelativeOrAbsolute, out _))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid URI format."));
						}
						break;
					case "ipv4":
						if (!IPAddress.TryParse(instance, out var ipv4) || ipv4.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid IPv4 address."));
						}
						break;
					case "ipv6":
						if (!IPAddress.TryParse(instance, out var ipv6) || ipv6.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid IPv6 address."));
						}
						break;
					case "uuid":
					case "guid":
						if (!Guid.TryParse(instance, out _))
						{
							errors.Add(new JsonSchemaValidationError(path, "format", "String is not a valid UUID/GUID format."));
						}
						break;

					default:
						throw new ArgumentNullException($"Unsupported string format: {format}");
				}
			}
		}

		private void ValidateNumber(double instance, JsonObject nodeSchema, string path, List<JsonSchemaValidationError> errors)
		{
			if (nodeSchema["minimum"].TryGet<double>() is double { } minimum && instance <= minimum)
			{
				errors.Add(new JsonSchemaValidationError(path, "minimum", $"Number must be at least {minimum}, but is {instance}."));
			}
			if (nodeSchema["exclusiveMinimum"].TryGet<double>() is double { } exclusiveMinimum && instance < exclusiveMinimum)
			{
				errors.Add(new JsonSchemaValidationError(path, "minimum", $"Number must be at greater than {exclusiveMinimum}, but is {instance}."));
			}
			if (nodeSchema["maximum"].TryGet<double>() is double { } maximum && instance >= maximum)
			{
				errors.Add(new JsonSchemaValidationError(path, "minimum", $"Number must be at most {maximum}, but is {instance}."));
			}
			if (nodeSchema["exclusiveMaximum"].TryGet<double>() is double { } exclusiveMaximum && instance > exclusiveMaximum)
			{
				errors.Add(new JsonSchemaValidationError(path, "minimum", $"Number must be at less than {exclusiveMaximum}, but is {instance}."));
			}
			if (nodeSchema["multipleOf"].TryGet<double>() is double { } multipleOf && instance % multipleOf != 0)
			{
				errors.Add(new JsonSchemaValidationError(path, "minimum", $"Number must be a multiple of {multipleOf}."));
			}
		}

		/// <inheritdoc/>
		public static JsonValue SerializeIntoJson(JsonSchema self, JsonOptions options)
		{
			return self.schema;
		}

		/// <inheritdoc/>
		public static JsonSchema DeserializeFromJson(JsonValue json, JsonOptions options)
		{
			return new JsonSchema(json.GetJsonObject());
		}

		/// <inheritdoc/>
		public string ToString(JsonOptions options)
		{
			return this.schema.ToString(options);
		}

		/// <inheritdoc/>
		public JsonValue AsJsonValue()
		{
			return this.schema.AsJsonValue();
		}
	}
}