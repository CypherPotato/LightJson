using LightJson.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LightJson;

using System.Diagnostics.CodeAnalysis;
using System.Text;

/// <summary>
/// Provides useful JSON extension methods for various classes.
/// </summary>
public static class JsonValueExtensions
{
	/// <summary>
	/// Reads an <see cref="HttpContent"/> as a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="encoding">The encoding to use. If <see langword="null"/>, UTF-8 is used.</param>
	/// <param name="options">The JSON options to use. If <see langword="null"/>, <see cref="JsonOptions.Default"/> is used.</param>
	/// <returns>The parsed <see cref="JsonValue"/>.</returns>
	public static JsonValue ReadAsJsonValue(this HttpContent httpContent, Encoding? encoding = null, JsonOptions? options = null)
	{

		var responseMessageStream = httpContent.ReadAsStream();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);

		return jr.Parse();
	}

	/// <summary>
	/// Reads an <see cref="HttpContent"/> as a <typeparamref name="TResult"/>.
	/// </summary>
	/// <typeparam name="TResult">The type to deserialize the JSON to.</typeparam>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="encoding">The encoding to use. If <see langword="null"/>, UTF-8 is used.</param>
	/// <param name="options">The JSON options to use. If <see langword="null"/>, <see cref="JsonOptions.Default"/> is used.</param>
	/// <returns>The deserialized object of type <typeparamref name="TResult"/>.</returns>
	public static TResult ReadAsJsonValue<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TResult>(this HttpContent httpContent, Encoding? encoding = null, JsonOptions? options = null)
	{

		var responseMessageStream = httpContent.ReadAsStream();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);

		var json = jr.Parse();
		return json.Get<TResult>();
	}

	/// <summary>
	/// Asynchronously reads an <see cref="HttpContent"/> as a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="encoding">The encoding to use. If <see langword="null"/>, UTF-8 is used.</param>
	/// <param name="options">The JSON options to use. If <see langword="null"/>, <see cref="JsonOptions.Default"/> is used.</param>
	/// <param name="cancellation">A cancellation token to observe.</param>
	/// <returns>A task that represents the asynchronous read operation. The task result contains the parsed <see cref="JsonValue"/>.</returns>
	public static async Task<JsonValue> ReadAsJsonValueAsync(this HttpContent httpContent, Encoding? encoding = null, JsonOptions? options = null, CancellationToken cancellation = default)
	{
		if (httpContent.Headers.ContentType?.CharSet is { } encodingCharset)
		{
			encoding = Encoding.GetEncoding(encodingCharset);
		}

		var responseMessageStream = await httpContent.ReadAsStreamAsync();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);

		return await jr.ParseAsync(cancellation);
	}

	/// <summary>
	/// Asynchronously reads an <see cref="HttpContent"/> as a <typeparamref name="TObject"/>.
	/// </summary>
	/// <typeparam name="TObject">The type to deserialize the JSON to.</typeparam>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="encoding">The encoding to use. If <see langword="null"/>, UTF-8 is used.</param>
	/// <param name="options">The JSON options to use. If <see langword="null"/>, <see cref="JsonOptions.Default"/> is used.</param>
	/// <param name="cancellation">A cancellation token to observe.</param>
	/// <returns>A task that represents the asynchronous read operation. The task result contains the deserialized object of type <typeparamref name="TObject"/>.</returns>
	public static async Task<TObject> ReadAsJsonValueAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TObject>(this HttpContent httpContent, Encoding? encoding = null, JsonOptions? options = null, CancellationToken cancellation = default)
	{
		if (httpContent.Headers.ContentType?.CharSet is { } encodingCharset)
		{
			encoding = Encoding.GetEncoding(encodingCharset);
		}

		var responseMessageStream = await httpContent.ReadAsStreamAsync();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);

		JsonValue value = await jr.ParseAsync(cancellation);
		return value.Get<TObject>();
	}
}