using LightJson.Serialization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LightJson;

/// <summary>
/// Provides useful JSON extension methods for various classes.
/// </summary>
public static class JsonValueExtensions
{
	/// <summary>
	/// Serializes the HTTP content and returns an <see cref="JsonValue"/> that represents
	/// the content.
	/// </summary>
	/// <param name="httpContent">The HTTP content object.</param>
	/// <param name="encoding">Optional. The encoding used for the decoding.</param>
	/// <param name="options">Optional. The <see cref="JsonOptions"/> object for the serializer.</param>
	public static JsonValue ReadAsJsonValue(this HttpContent httpContent,
		Encoding? encoding = null,
		JsonOptions? options = null)
	{
		var responseMessageStream = httpContent.ReadAsStream();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);
		return jr.Parse();
	}

	/// <summary>
	/// Serializes the HTTP content and returns an <see cref="JsonValue"/> that represents
	/// the content as an asynchronous operation.
	/// </summary>
	/// <param name="httpContent">The HTTP content object.</param>
	/// <param name="encoding">Optional. The encoding used for the decoding.</param>
	/// <param name="options">Optional. The <see cref="JsonOptions"/> object for the serializer.</param>
	public static async Task<JsonValue> ReadAsJsonValueAsync(this HttpContent httpContent,
		Encoding? encoding = null,
		JsonOptions? options = null)
	{
		var responseMessageStream = await httpContent.ReadAsStreamAsync();
		using StreamReader sr = new StreamReader(responseMessageStream, encoding ?? Encoding.UTF8);
		using JsonReader jr = new JsonReader(sr, options ?? JsonOptions.Default);
		return await jr.ParseAsync();
	}
}
