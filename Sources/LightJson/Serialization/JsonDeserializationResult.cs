using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Serialization;

/// <summary>
/// Represents the result of a JSON deserialization operation.
/// </summary>
/// <param name="error">The error message if the deserialization failed; otherwise; null.</param>
/// <param name="result">The resulting JSON value.</param>
public sealed class JsonDeserializationResult(string? error, JsonValue result)
{
	/// <summary>
	/// Gets a value indicating whether the deserialization was successful.
	/// </summary>
	public bool Success { get; } = error == null;

	/// <summary>
	/// Gets the error message if the deserialization failed; otherwise, null.
	/// </summary>
	public string? Error { get; } = error;

	/// <summary>
	/// Gets the resulting JSON value.
	/// </summary>
	public JsonValue Result { get; } = result;

	/// <summary>
	/// Gets the type of the resulting JSON value.
	/// </summary>
	public JsonValueType ResultType { get => this.Result.Type; }
}