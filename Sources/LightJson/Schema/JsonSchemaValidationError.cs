using System.Text;

namespace LightJson.Schema
{
	/// <summary>
	/// Represents a single validation error when validating a JsonValue against a JsonSchema.
	/// </summary>
	/// <param name="path">The path to the failing JSON value.</param>
	/// <param name="keyword">The schema keyword that failed.</param>
	/// <param name="message">The validation error message.</param>
	public sealed class JsonSchemaValidationError(string path, string keyword, string message)
	{
		/// <summary>
		/// Gets the path to the JSON value that failed validation.
		/// </summary>
		public string Path { get; } = path;

		/// <summary>
		/// Gets the schema keyword that was not satisfied.
		/// </summary>
		public string Keyword { get; } = keyword;

		/// <summary>
		/// Gets a detailed message explaining the validation error.
		/// </summary>
		public string Message { get; } = message;

		/// <summary>
		/// Returns a string representation of the validation error.
		/// </summary>
		public override string ToString()
		{
			return $"At {this.Path}: [{this.Keyword}] {this.Message}";
		}
	}
}
