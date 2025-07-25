using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson;

/// <summary>
/// Represents the result of a JSON structure validation.
/// </summary>
/// <param name="Success">A value indicating whether the validation was successful.</param>
/// <param name="Error">The error message if the validation failed, or <c>null</c> otherwise.</param>
/// <param name="ErrorPath">The path to the error location if the validation failed, or <c>null</c> otherwise.</param>
public record JsonStructureValidationResult(bool Success, string? Error, string? ErrorPath);

/// <summary>
/// Validates the structure and format of two JSON values.
/// </summary>
public sealed class JsonStructureValidator
{
	/// <summary>
	/// Gets or sets a value indicating whether empty arrays are allowed.
	/// </summary>
	public bool AllowEmptyArrays { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether null values are allowed.
	/// </summary>
	public bool AllowNullValues { get; set; } = true;

	/// <summary>
	/// Gets or sets whether the validator should ignore empty objects during validation.
	/// </summary>
	public bool IgnoreEmptyObjects { get; set; } = true;

	/// <summary>
	/// Checks the structure of the provided <paramref name="input"/> against the given <paramref name="schema"/>.
	/// </summary>
	/// <param name="input">The JSON value to validate.</param>
	/// <param name="schema">The schema to validate against.</param>
	/// <returns>A <see cref="JsonStructureValidationResult"/> indicating the result of the validation.</returns>
	public JsonStructureValidationResult CheckStructure(in JsonValue input, in JsonValue schema)
	{
		JsonStructureValidationResult ConfirmJsonObject(JsonObject inputObj, JsonObject schemaObj)
		{
			if (this.IgnoreEmptyObjects && schemaObj.Count == 0)
				return new JsonStructureValidationResult(true, null, null);
			if (inputObj.Count != schemaObj.Count)
				return new JsonStructureValidationResult(false, $"The count of properties of the input object ({inputObj.Count} properties) is different than the schema provided object ({schemaObj.Count} properties).", inputObj.path);

			foreach (var propertyA in inputObj)
			{
				var propertyB = schemaObj[propertyA.Key];

				if (!propertyB.IsDefined)
				{
					return new JsonStructureValidationResult(false, "The schema didn't expected this property to exist.", propertyA.Value.Path);
				}
				else if (propertyA.Value.Type != propertyB.Type)
				{
					if (this.AllowNullValues && (propertyA.Value.IsNull || propertyB.IsNull))
					{
						return new JsonStructureValidationResult(true, null, null);
					}
					else
					{
						return new JsonStructureValidationResult(false, $"The property value is null while the schema expected an {propertyB.Type}.", propertyA.Value.Path);
					}
				}

				if (propertyA.Value.Type == JsonValueType.Object)
				{
					if (ConfirmJsonObject(propertyA.Value.GetJsonObject(), propertyB.GetJsonObject()) is { Success: false } badResponse)
					{
						return badResponse;
					}
				}
				else
				{
					if (ConfirmJsonValue(propertyA.Value, propertyB) is { Success: false } badResponse)
					{
						return badResponse;
					}
				}
			}

			return new JsonStructureValidationResult(true, null, null);
		}

		JsonStructureValidationResult ConfirmJsonValue(in JsonValue input, in JsonValue schema)
		{
			if (input.Type != schema.Type)
			{
				return new JsonStructureValidationResult(false, $"The type of objects are different. The input object is {input.Type}, while the expected schema value is {schema.Type}.", input.Path);
			}
			else if (input.Type == JsonValueType.Object)
			{
				return ConfirmJsonObject(input.GetJsonObject(), schema.GetJsonObject());
			}
			else if (input.Type == JsonValueType.Array)
			{
				var arr1 = input.GetJsonArray();
				var arr2 = schema.GetJsonArray();

				if (arr1.Count > 0 && arr2.Count > 0)
				{
					if (arr1[0].Type == JsonValueType.Object)
					{
						if (arr2[0].Type == JsonValueType.Object)
						{
							return ConfirmJsonObject(arr1[0].GetJsonObject(), arr2[0].GetJsonObject());
						}
						else
						{
							return new JsonStructureValidationResult(false, $"The type of objects are different. The input object is {input.Type}, while the expected schema value is {schema.Type}.", input.Path);
						}
					}
					else
					{
						return ConfirmJsonValue(input[0], schema[0]);
					}
				}
				else if (this.AllowEmptyArrays)
				{
					return new JsonStructureValidationResult(true, null, null);
				}
				else
				{
					return new JsonStructureValidationResult(false, "The schema doens't allows empty arrays.", arr1.path);
				}
			}
			else
			{
				if (this.AllowNullValues && (input.IsNull || schema.IsNull))
				{
					return new JsonStructureValidationResult(false, $"The property value is null while the schema expected an {schema.Type}.", input.Path);
				}
				else
				{
					if (input.Type == schema.Type)
					{
						return new JsonStructureValidationResult(true, null, null);
					}
					else
					{
						return new JsonStructureValidationResult(false, $"The type of objects are different. The input object is {input.Type}, while the expected schema value is {schema.Type}.", input.Path);
					}
				}
			}
		}

		return ConfirmJsonValue(input, schema);
	}

	/// <summary>
	/// Checks if the structure of the provided <paramref name="input"/> is equal to the given <paramref name="schema"/>.
	/// </summary>
	/// <param name="input">The JSON value to validate.</param>
	/// <param name="schema">The schema to validate against.</param>
	/// <returns><c>true</c> if the structure is equal; otherwise, <c>false</c>.</returns>
	public bool StructureEquals(in JsonValue input, in JsonValue schema)
	{
		return this.CheckStructure(input, schema).Success;
	}
}
