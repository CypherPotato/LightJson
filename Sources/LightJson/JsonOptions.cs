using LightJson.Converters;
using LightJson.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonConverter = LightJson.Converters.JsonConverter;

namespace LightJson;

#nullable enable

/// <summary>
/// Provides options and configurations for using the LightJson library.
/// </summary>
public sealed class JsonOptions
{
	private readonly static JsonOptions _default;

	internal static JsonConverter[] RequiredConverters;

	static JsonOptions()
	{
		RequiredConverters = [
			//new DictionaryConverter(),
			new GuidConverter(),
			new EnumConverter(),
			new DateTimeConverter(),
			new DateOnlyConverter(),
			new TimeOnlyConverter(),
			new TimeSpanConverter(),
			new CharConverter(),
			new IpAddressConverter(),
			new UriConverter()
		];
		_default = new JsonOptions();
	}

	/// <summary>
	/// Gets the default <see cref="JsonOptions"/> object.
	/// </summary>
	public static JsonOptions Default { get => _default; }

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
	/// Gets or sets an list of <see cref="JsonConverter"/>.
	/// </summary>
	public IList<JsonConverter> Converters { get; set; }

	/// <summary>
	/// Gets or sets the function that transforms the property name of a JSON object to JSON output.
	/// </summary>
	public JsonNamingPolicy? NamingPolicy { get; set; }

	/// <summary>
	/// Gets or sets the encoder to use when escaping strings, or <see langword="null"/> to use the default encoder.
	/// </summary>
	public JavaScriptEncoder? StringEncoder { get; set; }

	/// <summary>
	/// Gets or sets an boolean indicating where the JSON parser should throw on duplicated object keys.
	/// </summary>
	public bool ThrowOnDuplicateObjectKeys { get; set; } = false;

	/// <summary>
	/// Gets or sets the maximum depth for serializing or deserializing objects.
	/// </summary>
	public int DynamicObjectMaxDepth { get; set; } = 64;

	/// <summary>
	/// Gets or sets whether the JSON deserializer should allow parsing string JSON values into numeric
	/// types.
	/// </summary>
	public bool AllowNumbersAsStrings { get; set; } = false;

	/// <summary>
	/// Gets or sets the context for serializing and deserializing JSON data with options.
	/// </summary>
	public JsonOptionsSerializerContext? SerializerContext { get; set; }

	/// <summary>
	/// Gets or sets the output for the JSON writer when writing double Infinity numbers.
	/// </summary>
	public JsonInfinityHandleOption InfinityHandler { get; set; } = JsonInfinityHandleOption.WriteNull;

	/// <summary>
	/// Creates an new <see cref="JsonOptions"/> instance with default parameters.
	/// </summary>
	public JsonOptions()
	{
		this.Converters = [.. RequiredConverters];

		if (RuntimeFeature.IsDynamicCodeSupported)
		{
#pragma warning disable IL2026, IL3050
			this.SerializerContext = new JsonOptionsSerializerContext();
#pragma warning restore IL2026, IL3050
		}
	}

	/// <summary>
	/// Creates an new <see cref="JsonOptions"/> instance with predefined set of options.
	/// </summary>
	/// <param name="defaults">The <see cref="JsonOptionsDefaults"/> to inherit properties.</param>
	public JsonOptions(JsonOptionsDefaults defaults) : this()
	{
		if (defaults == JsonOptionsDefaults.Web)
		{
			this.NamingPolicy = JsonNamingPolicy.CamelCase;
			this.PropertyNameComparer = JsonSanitizedComparer.Instance;
			this.AllowNumbersAsStrings = true;
		}
		else if (defaults != JsonOptionsDefaults.General)
		{
			throw new ArgumentOutOfRangeException(nameof(defaults));
		}
	}

	/// <summary>
	/// Creates a new <see cref="JsonOptions"/> instance by copying properties from an existing <see cref="JsonOptions"/> instance.
	/// </summary>
	/// <param name="options">The <see cref="JsonOptions"/> instance to copy properties from.</param>
	public JsonOptions(JsonOptions options) : this()
	{
		this.SerializationFlags = options.SerializationFlags;
		this.WriteIndented = options.WriteIndented;
		this.PropertyNameComparer = options.PropertyNameComparer;
		this.Converters = options.Converters;
		this.NamingPolicy = options.NamingPolicy;
		this.StringEncoder = options.StringEncoder;
		this.ThrowOnDuplicateObjectKeys = options.ThrowOnDuplicateObjectKeys;
		this.DynamicObjectMaxDepth = options.DynamicObjectMaxDepth;
		this.AllowNumbersAsStrings = options.AllowNumbersAsStrings;
		this.SerializerContext = options.SerializerContext;
		this.InfinityHandler = options.InfinityHandler;
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

	#region Serialize methods
	/// <summary>
	/// Serializes the specified object to a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="value">The object to serialize. Can be null.</param>
	/// <returns>A <see cref="JsonValue"/> representing the serialized object.</returns>
	public JsonValue Serialize(object? value)
	{
		JsonValue _value = LightJson.Serialization.JsonSerializer.SerializeObject(value, 0, true, this);
		_value.options = this;
		return _value;
	}

	/// <summary>
	/// Serializes the specified object to a <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="value">The object to serialize. Can be null.</param>
	/// <param name="enableConverters">A boolean value that indicates whether to enable converters during serialization.</param>
	/// <returns>A <see cref="JsonValue"/> representing the serialized object.</returns>
	public JsonValue Serialize(object? value, bool enableConverters = true)
	{
		JsonValue _value = LightJson.Serialization.JsonSerializer.SerializeObject(value, 0, enableConverters, this);
		_value.options = this;
		return _value;
	}

	/// <summary>
	/// Serializes the specified <see cref="IJsonSerializable{TJsonSerializable}"/> object to a <see cref="JsonValue"/>.
	/// </summary>
	/// <typeparam name="TJsonSerializable">The type of the object to serialize, which must implement <see cref="IJsonSerializable{TJsonSerializable}"/>.</typeparam>
	/// <param name="value">The object to serialize.</param>
	/// <returns>A <see cref="JsonValue"/> representing the serialized object.</returns>
	public JsonValue Serialize<TJsonSerializable>(TJsonSerializable value) where TJsonSerializable : IJsonSerializable<TJsonSerializable>
	{
		JsonValue _value = TJsonSerializable.SerializeIntoJson(value, this);
		_value.options = this;
		return _value;
	}

	/// <summary>
	/// Serializes the specified object into an JSON string.
	/// </summary>
	/// <param name="value">The object to serialize into an JSON string.</param>
	public string SerializeJson(object? value) => this.Serialize(value).ToString();

	/// <summary>
	/// Serializes the specified object into a UTF-8 encoded byte array.
	/// </summary>
	/// <param name="value">The object to serialize into a UTF-8 encoded byte array.</param>
	/// <returns>A UTF-8 encoded byte array representing the serialized object.</returns>
	public byte[] SerializeUtf8Bytes(object? value)
	{
		string jsonText = this.Serialize(value).ToString();
		return Encoding.UTF8.GetBytes(jsonText);
	}
	#endregion

	#region Deserialize methods
	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="sr"/> is null.</exception>
	public JsonValue Deserialize(TextReader sr)
	{
		ArgumentNullException.ThrowIfNull(sr);

		using var jr = new JsonReader(sr, this);
		return jr.Parse();
	}

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public JsonValue Deserialize(string jsonText)
	{
		ArgumentNullException.ThrowIfNull(jsonText);

		return this.Deserialize(new StringReader(jsonText));
	}

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="utf8JsonString"/> read-only span of characters.
	/// </summary>
	/// <param name="utf8JsonString">The read-only span of characters containing the JSON data to deserialize.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	public JsonValue Deserialize(in ReadOnlySpan<char> utf8JsonString)
	{
		return this.Deserialize(new StringReader(new string(utf8JsonString)));
	}

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="jsonText"/> read-only span of bytes.
	/// </summary>
	/// <param name="jsonText">The read-only span of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	public JsonValue Deserialize(in ReadOnlySpan<byte> jsonText, Encoding? encoding = null)
	{
		return this.Deserialize(new StringReader((encoding ?? Encoding.UTF8).GetString(jsonText)));
	}

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="inputStream"/> stream.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream.</param>
	/// <param name="leaveOpen">Whether to leave the stream open after deserialization. Defaults to false.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inputStream"/> or <paramref name="encoding"/> is null.</exception>
	public JsonValue Deserialize(Stream inputStream, Encoding? encoding, bool leaveOpen = false)
	{
		ArgumentNullException.ThrowIfNull(inputStream);
		ArgumentNullException.ThrowIfNull(encoding);

		using var sr = new StreamReader(inputStream, encoding ?? Encoding.UTF8, leaveOpen);
		using var jr = new JsonReader(sr, this, leaveOpen);
		return jr.Parse();
	}

	/// <summary>
	/// Deserializes a JSON string into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="jsonText">The JSON string to deserialize.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string jsonText) where T : notnull => this.Deserialize(jsonText).Get<T>();

	/// <summary>
	/// Deserializes a JSON string from a read-only span of characters into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="utf8JsonString">The read-only span of characters containing the JSON data to deserialize.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(in ReadOnlySpan<char> utf8JsonString) where T : notnull => this.Deserialize(utf8JsonString).Get<T>();

	/// <summary>
	/// Deserializes a JSON string from a read-only span of bytes into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="utf8JsonString">The read-only span of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(in ReadOnlySpan<byte> utf8JsonString, Encoding? encoding = null) where T : notnull => this.Deserialize(utf8JsonString, encoding).Get<T>();

	/// <summary>
	/// Deserializes a JSON string from a <see cref="TextReader"/> into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="reader">The <see cref="TextReader"/> containing the JSON data to deserialize.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(TextReader reader) where T : notnull => this.Deserialize(reader).Get<T>();

	/// <summary>
	/// Deserializes a JSON string from a <see cref="Stream"/> into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="inputStream">The <see cref="Stream"/> containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Stream inputStream, Encoding? encoding) where T : notnull => this.Deserialize(inputStream, encoding).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="sr"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(TextReader sr, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sr);

		using var jr = new JsonReader(sr, this);
		return await jr.ParseAsync(cancellation);
	}

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(string jsonText, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(jsonText);

		return await this.DeserializeAsync(new StringReader(jsonText), cancellation);
	}

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="inputStream"/> stream.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="inputStream"/> or <paramref name="encoding"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(Stream inputStream, Encoding? encoding, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(inputStream);
		ArgumentNullException.ThrowIfNull(encoding);

		using var sr = new StreamReader(inputStream, encoding ?? Encoding.UTF8);
		using var jr = new JsonReader(sr, this);
		return await jr.ParseAsync(cancellation);
	}

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="jsonText"/> read-only memory of bytes.
	/// </summary>
	/// <param name="jsonText">The read-only memory of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(ReadOnlyMemory<byte> jsonText, Encoding? encoding, CancellationToken cancellation = default)
	{
		return await this.DeserializeAsync(new StringReader((encoding ?? Encoding.UTF8).GetString(jsonText.Span)), cancellation);
	}

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="utf8JsonString"/> read-only memory of characters.
	/// </summary>
	/// <param name="utf8JsonString">The read-only memory of characters containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="utf8JsonString"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(ReadOnlyMemory<char> utf8JsonString, CancellationToken cancellation = default)
	{
		return await this.DeserializeAsync(new StringReader(new string(utf8JsonString.Span)), cancellation);
	}

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="reader"/> text reader into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="reader">The text reader containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning an instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public async Task<T> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(TextReader reader, CancellationToken cancellation = default) where T : notnull => (await this.DeserializeAsync(reader, cancellation)).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="jsonText"/> string into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning an instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public async Task<T> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string jsonText, CancellationToken cancellation = default) where T : notnull => (await this.DeserializeAsync(jsonText, cancellation)).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="jsonText"/> read-only memory of bytes into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="jsonText">The read-only memory of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning an instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public async Task<T> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ReadOnlyMemory<byte> jsonText, Encoding? encoding, CancellationToken cancellation = default) where T : notnull => (await this.DeserializeAsync(jsonText, encoding, cancellation)).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="utf8JsonString"/> read-only memory of characters into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="utf8JsonString">The read-only memory of characters containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning an instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public async Task<T> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(ReadOnlyMemory<char> utf8JsonString, CancellationToken cancellation = default) where T : notnull => (await this.DeserializeAsync(utf8JsonString, cancellation)).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="inputStream"/> stream into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="inputStream">The stream containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning an instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public async Task<T> DeserializeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(Stream inputStream, Encoding? encoding, CancellationToken cancellation = default) where T : notnull => (await this.DeserializeAsync(inputStream, encoding, cancellation)).Get<T>();
	#endregion

	#region TryDeserialize methods
	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="inputStream"/> stream.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream. Defaults to <see cref="Encoding.Default"/> if null.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	public bool TryDeserialize(Stream inputStream, Encoding? encoding, out JsonValue result)
	{
		using var sr = new StreamReader(inputStream, encoding ?? Encoding.Default);
		using var jr = new JsonReader(sr, this);
		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	public bool TryDeserialize(TextReader sr, out JsonValue result)
	{
		using var jr = new JsonReader(sr, this);
		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public bool TryDeserialize(string jsonText, out JsonValue result)
	{
		ArgumentNullException.ThrowIfNull(jsonText);
		using var sr = new StringReader(jsonText);
		using var jr = new JsonReader(sr, this);

		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="utf8JsonString"/> read-only span of characters.
	/// </summary>
	/// <param name="utf8JsonString">The read-only span of characters containing the JSON data to deserialize.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	public bool TryDeserialize(in ReadOnlySpan<char> utf8JsonString, out JsonValue result)
	{
		using var sr = new StringReader(new string(utf8JsonString));
		using var jr = new JsonReader(sr, this);

		return jr.TryParse(out result);
	}

	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> read-only span of bytes.
	/// </summary>
	/// <param name="jsonText">The read-only span of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	public bool TryDeserialize(in ReadOnlySpan<byte> jsonText, Encoding? encoding, out JsonValue result)
	{
		using var sr = new StringReader((encoding ?? Encoding.UTF8).GetString(jsonText));
		using var jr = new JsonReader(sr, this);

		return jr.TryParse(out result);
	}

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="reader"/> text reader.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<(bool, JsonValue)> TryDeserializeAsync(TextReader reader, CancellationToken cancellation = default)
	{
		using var jr = new JsonReader(reader, this);
		return jr.TryParseAsync(cancellation);
	}

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="inputStream"/> stream.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when reading the stream. Defaults to <see cref="Encoding.Default"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<(bool, JsonValue)> TryDeserializeAsync(Stream inputStream, Encoding? encoding, CancellationToken cancellation = default)
	{
		return this.TryDeserializeAsync(new StreamReader(inputStream, encoding ?? Encoding.Default), cancellation);
	}

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<(bool, JsonValue)> TryDeserializeAsync(string jsonText, CancellationToken cancellation = default)
	{
		return this.TryDeserializeAsync(new StringReader(jsonText), cancellation);
	}

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> read-only memory of characters.
	/// </summary>
	/// <param name="jsonText">The read-only memory of characters containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<(bool, JsonValue)> TryDeserializeAsync(ReadOnlyMemory<char> jsonText, CancellationToken cancellation = default)
	{
		return this.TryDeserializeAsync(new StringReader(new string(jsonText.Span)), cancellation);
	}

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> read-only memory of bytes.
	/// </summary>
	/// <param name="jsonText">The read-only memory of bytes containing the JSON data to deserialize.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<(bool, JsonValue)> TryDeserializeAsync(ReadOnlyMemory<byte> jsonText, Encoding? encoding, CancellationToken cancellation = default)
	{
		return this.TryDeserializeAsync(new StringReader((encoding ?? Encoding.UTF8).GetString(jsonText.Span)), cancellation);
	}

	#endregion

	#region IsValid methods
	/// <summary>
	/// Determines whether the specified <paramref name="jsonText"/> string is valid JSON.
	/// </summary>
	/// <param name="jsonText">The string to check for validity.</param>
	/// <returns>True if the string is valid JSON; otherwise, false.</returns>
	public bool IsValidJson(string jsonText)
	{
		return this.TryDeserialize(jsonText, out _);
	}

	/// <summary>
	/// Determines whether the specified <paramref name="jsonText"/> read-only span of characters is valid JSON.
	/// </summary>
	/// <param name="jsonText">The read-only span of characters to check for validity.</param>
	/// <returns>True if the span is valid JSON; otherwise, false.</returns>
	public bool IsValidJson(in ReadOnlySpan<char> jsonText)
	{
		return this.TryDeserialize(jsonText, out _);
	}

	/// <summary>
	/// Determines whether the specified <paramref name="jsonText"/> read-only span of bytes is valid JSON.
	/// </summary>
	/// <param name="jsonText">The read-only span of bytes to check for validity.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <returns>True if the span is valid JSON; otherwise, false.</returns>
	public bool IsValidJson(in ReadOnlySpan<byte> jsonText, Encoding? encoding)
	{
		return this.TryDeserialize(jsonText, encoding, out _);
	}

	/// <summary>
	/// Determines whether the JSON data in the specified <paramref name="reader"/> text reader is valid.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to check for validity.</param>
	/// <returns>True if the JSON data is valid; otherwise, false.</returns>
	public bool IsValidJson(TextReader reader)
	{
		return this.TryDeserialize(reader, out _);
	}

	/// <summary>
	/// Determines whether the JSON data in the specified <paramref name="inputStream"/> stream is valid.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to check for validity.</param>
	/// <param name="encoding">The encoding to use when reading the stream.</param>
	/// <returns>True if the JSON data is valid; otherwise, false.</returns>
	public bool IsValidJson(Stream inputStream, Encoding? encoding)
	{
		return this.TryDeserialize(inputStream, encoding, out _);
	}

	/// <summary>
	/// Asynchronously determines whether the specified <paramref name="jsonText"/> string is valid JSON.
	/// </summary>
	/// <param name="jsonText">The string to check for validity.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the string is valid JSON; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(string jsonText, CancellationToken cancellation = default)
	{
		return (await this.TryDeserializeAsync(jsonText, cancellation).ConfigureAwait(false)).Item1;
	}

	/// <summary>
	/// Asynchronously determines whether the JSON data in the specified <paramref name="reader"/> text reader is valid.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to check for validity.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the JSON data is valid; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(TextReader reader, CancellationToken cancellation = default)
	{
		return (await this.TryDeserializeAsync(reader, cancellation).ConfigureAwait(false)).Item1;
	}

	/// <summary>
	/// Asynchronously determines whether the JSON data in the specified <paramref name="inputStream"/> stream is valid.
	/// </summary>
	/// <param name="inputStream">The stream containing the JSON data to check for validity.</param>
	/// <param name="encoding">The encoding to use when reading the stream. Defaults to <see cref="Encoding.Default"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the JSON data is valid; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(Stream inputStream, Encoding? encoding, CancellationToken cancellation = default)
	{
		return (await this.TryDeserializeAsync(inputStream, encoding, cancellation).ConfigureAwait(false)).Item1;
	}

	/// <summary>
	/// Asynchronously determines whether the specified <paramref name="jsonText"/> read-only memory of bytes is valid JSON.
	/// </summary>
	/// <param name="jsonText">The read-only memory of bytes to check for validity.</param>
	/// <param name="encoding">The encoding to use when converting the bytes to a string. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the span is valid JSON; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(ReadOnlyMemory<byte> jsonText, Encoding? encoding, CancellationToken cancellation = default)
	{
		return (await this.TryDeserializeAsync(jsonText, encoding, cancellation).ConfigureAwait(false)).Item1;
	}
	#endregion
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