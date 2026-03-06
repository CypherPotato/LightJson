using LightJson.Serialization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightJson;

/// <summary>
/// Represents HTTP content for JSON data, providing serialization of JSON values to HTTP message streams.
/// </summary>
public sealed class JsonContent : HttpContent
{
	static readonly Encoding encoding = new UTF8Encoding(false);
	private readonly JsonValue jsonValue;
	private readonly JsonOptions options;

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonContent"/> class with the specified content object,
	/// using default JSON options.
	/// </summary>
	/// <param name="content">The object to serialize as JSON content.</param>
	public JsonContent(object? content) : this(JsonOptions.Default.Serialize(content), JsonOptions.Default)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonContent"/> class with the specified JSON value,
	/// using default JSON options.
	/// </summary>
	/// <param name="jsonValue">The JSON value to be used as HTTP content.</param>
	public JsonContent(JsonValue jsonValue) : this(jsonValue, JsonOptions.Default)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonContent"/> class with the specified content object
	/// and custom JSON options.
	/// </summary>
	/// <param name="content">The object to serialize as JSON content.</param>
	/// <param name="options">The JSON serialization options to use.</param>
	public JsonContent(object? content, JsonOptions options) : this(options.Serialize(content), options)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="JsonContent"/> class with the specified JSON value
	/// and custom JSON options.
	/// </summary>
	/// <param name="jsonValue">The JSON value to be used as HTTP content.</param>
	/// <param name="options">The JSON serialization options to use.</param>
	public JsonContent(JsonValue jsonValue, JsonOptions options)
	{
		this.jsonValue = jsonValue;
		this.options = options;
		Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json", "utf-8");
	}

	/// <summary>
	/// Serializes the JSON content to the specified stream asynchronously.
	/// </summary>
	/// <param name="stream">The target stream to write the serialized JSON content.</param>
	/// <param name="context">The transport context information.</param>
	/// <returns>A task that represents the asynchronous serialization operation.</returns>
	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		using var writer = new StreamWriter(stream, encoding, 4096, true);
		using var jwriter = new JsonWriter(writer, options);

		jwriter.UseIndentedSyntax(false);
		jwriter.Write(jsonValue);

		await writer.FlushAsync().ConfigureAwait(false);
	}

	/// <summary>
	/// Serializes the JSON content to the specified stream asynchronously with cancellation support.
	/// </summary>
	/// <param name="stream">The target stream to write the serialized JSON content.</param>
	/// <param name="context">The transport context information.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous serialization operation.</returns>
	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		using var writer = new StreamWriter(stream, encoding, 4096, true);
		using var jwriter = new JsonWriter(writer, options);

		jwriter.UseIndentedSyntax(false);
		jwriter.Write(jsonValue);

		await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Attempts to compute the length of the HTTP content.
	/// </summary>
	/// <param name="length">When this method returns, contains the computed length of the HTTP content, or -1 if the length cannot be determined.</param>
	/// <returns><see langword="false"/> because the content length cannot be pre-computed for JSON serialization.</returns>
	protected override bool TryComputeLength(out long length)
	{
		length = -1;
		return false;
	}
}
