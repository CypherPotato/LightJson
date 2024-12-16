using LightJson.Converters;
using LightJson.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace LightJson;

#nullable enable

/// <summary>
/// Provides options and configurations for using the LightJson library.
/// </summary>
public sealed class JsonOptions
{
	private readonly static JsonOptions _default = new JsonOptions();

	/// <summary>
	/// Gets or sets the default <see cref="JsonOptions"/> object.
	/// </summary>
	public static JsonOptions Default { get; set; } = _default;

	/// <summary>
	/// Gets or sets serialization flags to the <see cref="Serialization.JsonReader"/>.
	/// </summary>
	public JsonSerializationFlags SerializationFlags { get; set; } = default;

	/// <summary>
	/// Gets or sets whether the JSON serializer should write indentend, pretty formatted
	/// outputs.
	/// </summary>
	public bool WriteIndented { get; set; } = false;

	/// <summary>
	/// Gets or sets the default string comparer used for comparing properties names.
	/// </summary>
	public StringComparer PropertyNameComparer { get; set; } = StringComparer.Ordinal;

	/// <summary>
	/// Gets or sets whether the <see cref="JsonValue.Serialize(object?, JsonOptions?)"/> should serialize fields from
	/// types.
	/// </summary>
	public bool SerializeFields { get; set; } = false;

	/// <summary>
	/// Gets or sets an list of <see cref="JsonConverter"/>.
	/// </summary>
	public JsonConverterCollection Converters { get; set; }

	/// <summary>
	/// Gets or sets the function that transforms the property name of a JSON object to JSON output.
	/// </summary>
	public JsonNamingPolicy? NamingPolicy { get; set; }

	/// <summary>
	/// Gets or sets an boolean indicating where the JSON parser should throw on duplicated object keys.
	/// </summary>
	public bool ThrowOnDuplicateObjectKeys { get; set; } = false;

	/// <summary>
	/// Gets or sets the maximum depth for serializing or deserializing dynamic objects.
	/// </summary>
	public int DynamicObjectMaxDepth { get; set; } = 64;

	/// <summary>
	/// Gets or sets the <see cref="DynamicSerializationMode"/> used by the JSON serializer.
	/// </summary>
	/// <remarks>
	/// Using <see cref="DynamicSerializationMode.Read"/> may require dynamic code which can be trimmed with
	/// trimmed/AOT compilation.
	/// </remarks>
	public DynamicSerializationMode DynamicSerialization { get; set; }

	/// <summary>
	/// Gets or sets the output for the JSON writer when writing double Infinity numbers.
	/// </summary>
	public JsonInfinityHandleOption InfinityHandler { get; set; } = JsonInfinityHandleOption.WriteNull;

	/// <summary>
	/// Creates an new <see cref="JsonOptions"/> instance with default parameters.
	/// </summary>
	public JsonOptions()
	{
		this.Converters = new JsonConverterCollection()
		{
			new DictionaryConverter(),
			new GuidConverter(),
			new EnumConverter(),
			new DateTimeConverter(),
			new DateOnlyConverter(),
			new TimeOnlyConverter(),
			new TimeSpanConverter(),
			new CharConverter()
		};

		if (RuntimeFeature.IsDynamicCodeSupported)
		{
			this.DynamicSerialization = DynamicSerializationMode.Both;
		}
		else
		{
			this.DynamicSerialization = DynamicSerializationMode.Write;
		}
	}

	/// <summary>
	/// Creates an new empty <see cref="JsonObject"/> instance using this <see cref="JsonOptions"/> 
	/// options.
	/// </summary>
	public JsonObject CreateJsonObject() => new JsonObject(this);

	/// <summary>
	/// Creates an new empty <see cref="JsonArray"/> instance using this <see cref="JsonOptions"/> 
	/// options.
	/// </summary>
	public JsonArray CreateJsonArray() => new JsonArray(this);

	/// <summary>
	/// Serializes the specified object to a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="value">The object to serialize. Can be null.</param>
	/// <returns>A <see cref="JsonValue"/> representing the serialized object.</returns>
	public JsonValue Serialize(object? value)
	{
		JsonValue _value = Dynamic.SerializeObject(value, 0, true, this, out JsonValueType valueType);
		_value.options = this;
		return _value;
	}

	/// <summary>
	/// Serializes the specified object into an JSON string.
	/// </summary>
	/// <param name="value">The object to serialize into an JSON string.</param>
	public string SerializeJson(object? value) => Serialize(value).ToString();

	/// <summary>
	/// Deserializes a JSON string into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="utf8JsonString">The JSON string to deserialize.</param>
	/// <returns>A <see cref="JsonValue"/> representing the deserialized JSON.</returns>
	public JsonValue Deserialize(string utf8JsonString)
	{
		ArgumentNullException.ThrowIfNull(utf8JsonString);
		using var sr = new StringReader(utf8JsonString);
		using var jr = new JsonReader(sr, this);
		return jr.Parse();
	}

	/// <summary>
	/// Deserializes a JSON string represented as a <see cref="ReadOnlySpan{Char}"/> into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="utf8JsonString">The JSON string to deserialize as a <see cref="ReadOnlySpan{Char}"/>.</param>
	/// <returns>A <see cref="JsonValue"/> representing the deserialized JSON.</returns>
	public JsonValue Deserialize(ReadOnlySpan<char> utf8JsonString)
	{
		return this.Deserialize(new string(utf8JsonString));
	}

	/// <summary>
	/// Deserializes JSON from a <see cref="TextReader"/> into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="sr">The <see cref="TextReader"/> containing the JSON data.</param>
	/// <returns>A <see cref="JsonValue"/> representing the deserialized JSON.</returns>
	public JsonValue Deserialize(TextReader sr)
	{
		using var jr = new JsonReader(sr, this);
		return jr.Parse();
	}

	/// <summary>
	/// Deserializes JSON from a stream into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data.</param>
	/// <param name="encoding">The encoding to use for reading the stream. If null, defaults to <see cref="Encoding.Default"/>.</param>
	/// <returns>A <see cref="JsonValue"/> representing the deserialized JSON.</returns>
	public JsonValue Deserialize(Stream inputStream, Encoding? encoding)
	{
		using var sr = new StreamReader(inputStream, encoding ?? Encoding.Default);
		using var jr = new JsonReader(sr, this);
		return jr.Parse();
	}

	/// <summary>
	/// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the object to deserialize to. Must not be null.</typeparam>
	/// <param name="utf8JsonString">The JSON string to deserialize.</param>
	/// <returns>An object of type <typeparamref name="T"/> representing the deserialized JSON.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string utf8JsonString) where T : notnull => this.Deserialize(utf8JsonString).Get<T>();

	/// <summary>
	/// Deserializes JSON from a stream into an object of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type of the object to deserialize to. Must not be null.</typeparam>
	/// <param name="inputStream">The stream containing the JSON data.</param>
	/// <param name="encoding">The encoding to use for reading the stream. If null, defaults to <see cref="Encoding.Default"/>.</param>
	/// <returns>An object of type <typeparamref name="T"/> representing the deserialized JSON.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Stream inputStream, Encoding? encoding) where T : notnull => this.Deserialize(inputStream, encoding).Get<T>();

	/// <summary>
	/// Attempts to deserialize JSON from a stream into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data.</param>
	/// <param name="encoding">The encoding to use for reading the stream. If null, defaults to <see cref="Encoding.Default"/>.</param>
	/// <param name="result">When this method returns, contains the deserialized <see cref="JsonValue"/> if successful; otherwise, the default value.</param>
	/// <returns><c>true</c> if the deserialization was successful; otherwise, <c>false</c>.</returns>
	public bool TryDeserialize(Stream inputStream, Encoding? encoding, out JsonValue result)
	{
		using var sr = new StreamReader(inputStream, encoding ?? Encoding.Default);
		using var jr = new JsonReader(sr, this);
		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize JSON from a <see cref="TextReader"/> into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="sr">The <see cref="TextReader"/> containing the JSON data.</param>
	/// <param name="result">When this method returns, contains the deserialized <see cref="JsonValue"/> if successful; otherwise, the default value.</param>
	/// <returns><c>true</c> if the deserialization was successful; otherwise, <c>false</c>.</returns>
	public bool TryDeserialize(TextReader sr, out JsonValue result)
	{
		using var jr = new JsonReader(sr, this);
		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="utf8Json">The JSON string to deserialize.</param>
	/// <param name="result">When this method returns, contains the deserialized <see cref="JsonValue"/> if successful; otherwise, the default value.</param>
	/// <returns><c>true</c> if the deserialization was successful; otherwise, <c>false</c>.</returns>
	public bool TryDeserialize(string utf8Json, out JsonValue result)
	{
		ArgumentNullException.ThrowIfNull(utf8Json);
		using var sr = new StringReader(utf8Json);
		using var jr = new JsonReader(sr, this);
		return jr.TryParse(out result);
	}
}

/// <summary>
/// Represents an JSON dynamic serialization mode.
/// </summary>
[Flags]
public enum DynamicSerializationMode
{
	/// <summary>
	/// Represents that the JSON serializer can write dynamic JSON for non-mapped objects.
	/// </summary>
	Write,

	/// <summary>
	/// Represents that the JSON serializer can read dynamic JSON for non-mapped objects.
	/// </summary>
	Read,

	/// <summary>
	/// Represents that the JSON serializer can read and write dynamic JSON for non-mapped objects.
	/// </summary>
	Both = Read | Write
}

/// <summary>
/// Represents the action to deal with float infinite numbers.
/// </summary>
public enum JsonInfinityHandleOption
{
	/// <summary>
	/// Write JSON null on Infinity numbers.
	/// </summary>
	WriteNull,

	/// <summary>
	/// Write JSON zero on Infinity numbers.
	/// </summary>
	WriteZero
}