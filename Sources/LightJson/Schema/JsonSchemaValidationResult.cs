using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LightJson.Schema
{
	/// <summary>
	/// Represents the result of a JSON Schema validation.
	/// </summary>
	public sealed class JsonSchemaValidationResult
	{
		/// <summary>
		/// Gets a value indicating whether the validation was successful.
		/// </summary>
		public bool IsValid => this.Errors.Count == 0;

		/// <summary>
		/// Gets the list of validation errors. This will be empty for successful validations.
		/// </summary>
		public IReadOnlyList<JsonSchemaValidationError> Errors { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSchemaValidationResult"/> class for a failed validation.
		/// </summary>
		/// <param name="errors">The list of validation errors.</param>
		public JsonSchemaValidationResult(IList<JsonSchemaValidationError> errors)
		{
			this.Errors = new ReadOnlyCollection<JsonSchemaValidationError>(errors ?? []);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JsonSchemaValidationResult"/> class for a successful validation.
		/// </summary>
		private JsonSchemaValidationResult()
		{
			this.Errors = [];
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Join("\n", this.Errors.Select(e => e.ToString()));
		}
	}
}
