using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightJson.Serialization;

/// <summary>
/// Represents a value that is serialized exactly as it is informed in the content.
/// </summary>
public sealed class JsonRawValue : IImplicitJsonValue
{
	internal static readonly string TAG = $"<<<JSON_RAW[[{Guid.NewGuid()}]]\n";

	/// <summary>
	/// Gets the raw content.
	/// </summary>
	public string? Content { get; }

	/// <summary>
	/// Creates an new instance of the <see cref="JsonRawValue"/>.
	/// </summary>
	/// <param name="content">The value that will be serialized.</param>
	public JsonRawValue(string content)
	{
		this.Content = content;
	}

	/// <summary>
	/// Creates an new instance of the <see cref="JsonRawValue"/>.
	/// </summary>
	/// <param name="content">The value that will be serialized.</param>
	public JsonRawValue(object? content)
	{
		this.Content = content?.ToString();
	}

	/// <inheritdoc/>
	public JsonValue AsJsonValue()
	{
		return new JsonValue($"{TAG}{this.Content}");
	}

	/// <inheritdoc/>
	public string ToString(JsonOptions options)
	{
		return options.SerializeJson(this);
	}
}
