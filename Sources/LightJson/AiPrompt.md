# The following document can be added to an AI-model context to hint it about the LightJson API.

## Basic serialization

```csharp
using LightJson;

// serialize primitive values
json = new JsonValue("hello").ToString();
Console.WriteLine(json); // "hello"

// serialize complex objects
json = JsonOptions.Default.Serialize(new { prop1 = "hello", prop2 = "world" }).ToString();
Console.WriteLine(json); // {"prop1":"hello","prop2":"world"}

// for custom types, an converter must be defined in the JsonOptions.Converters
json = JsonOptions.Default.Serialize(Guid.NewGuid()).ToString();
Console.WriteLine(json); // "9d282aa8-9385-4158-a094-55a01a39feae"

// deserialize primitive values
json = """
    {
        "number": 12.52,
        "name": "John Lennon",
        "arrayOfInts": [ 20, 30, 40 ],
        "object": {
            "guid": "9d282aa8-9385-4158-a094-55a01a39feae"
        }
    }
    """;

var objJson = JsonOptions.Default.Deserialize(json);
        
// implicitly converts the JsonValue into an JsonObject when acessing
// through key value
double objNumber = objJson["number"].GetNumber();

// MaybeNull() indicates that the value at $.name can be null
string? name = objJson["name"].MaybeNull()?.GetString();

// gets $.arrayOfInts[1] as integer. it must be an non-null number
int intAtIndex1 = objJson["arrayOfInts"][1].GetInteger();

// explicitly gets an Guid from $.object.guid
Guid convertedValue = objJson["object"]["guid"].Get<Guid>();
```

## Basic deserialization

```csharp
string json = """
    {
        "foobar": "hello",
        "bazdaz": null,
        "duzkaz": [
            "foo",
            "bar",
            "daz"
        ],
        "user": {
            "name": "John McAffee", "age": 52
        }
    }
    """;

var obj = JsonOptions.Default.Deserialize(json);

// $.foobar must be present, non null and carry an string value.
string stringValue = obj["foobar"].GetString();

// $.bazdaz can be null or undefined, but if not, it must be an string.
string? optionalValue = obj["bazdaz"].MaybeNull()?.GetString();

// $.duzkaz must be present, non null, be an json array and every children on it
// must be an string value.
string[] arrayItems = obj["duzkaz"].GetJsonArray().Select(i => i.GetString()).ToArray();
// or
string[] arrayItems = obj["duzkaz"].GetJsonArray().ToArray<string>();
// or
IEnumerable<string> arrayItems = obj["duzkaz"].GetJsonArray().EveryAs<string>();
// or
IEnumerable<string?> arrayItems = obj["duzkaz"].GetJsonArray().EveryAsNullable<string>();

// $.user must be present, non null, and must be converted to the User type, which it's converter
// is defined on JsonOptions.Converters.
User user = obj["user"].Get<User>();
// nullable
User? user = obj["user"].MaybeNull()?.Get<User>();
```

## JsonOptions

```csharp
namespace LightJson;

public sealed class JsonOptions {

	/// <summary>
	/// Gets the default <see cref="JsonOptions"/> object.
	/// </summary>
	public static JsonOptions Default { get; }

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
	public JsonValue Serialize(object? value);

    /// <summary>
	/// Serializes the specified <see cref="IJsonSerializable{TJsonSerializable}"/> object to a <see cref="JsonValue"/>.
	/// </summary>
	/// <typeparam name="TJsonSerializable">The type of the object to serialize, which must implement <see cref="IJsonSerializable{TJsonSerializable}"/>.</typeparam>
	/// <param name="value">The object to serialize.</param>
	/// <returns>A <see cref="JsonValue"/> representing the serialized object.</returns>
	public JsonValue Serialize<TJsonSerializable>(TJsonSerializable value) where TJsonSerializable : IJsonSerializable<TJsonSerializable>;

	/// <summary>
	/// Serializes the specified object into an JSON string.
	/// </summary>
	/// <param name="value">The object to serialize into an JSON string.</param>
	public string SerializeJson(object? value);

	/// <summary>
	/// Serializes the specified object into a UTF-8 encoded byte array.
	/// </summary>
	/// <param name="value">The object to serialize into a UTF-8 encoded byte array.</param>
	/// <returns>A UTF-8 encoded byte array representing the serialized object.</returns>
	public byte[] SerializeUtf8Bytes(object? value);

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="sr"/> is null.</exception>
	public JsonValue Deserialize(TextReader sr);

	/// <summary>
	/// Deserializes a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <returns>The deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public JsonValue Deserialize(string jsonText);

	/// <summary>
	/// Deserializes a JSON string into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="jsonText">The JSON string to deserialize.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(string jsonText) where T : notnull => this.Deserialize(jsonText).Get<T>();

	/// <summary>
	/// Deserializes a JSON string from a <see cref="TextReader"/> into an instance of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">The type to deserialize into.</typeparam>
	/// <param name="reader">The <see cref="TextReader"/> containing the JSON data to deserialize.</param>
	/// <returns>An instance of <typeparamref name="T"/> representing the deserialized JSON data.</returns>
	public T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(TextReader reader) where T : notnull => this.Deserialize(reader).Get<T>();

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="sr"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(TextReader sr, CancellationToken cancellation = default);

	/// <summary>
	/// Asynchronously deserializes a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning the deserialized <see cref="JsonValue"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public async Task<JsonValue> DeserializeAsync(string jsonText, CancellationToken cancellation = default);
	
	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="sr"/> text reader.
	/// </summary>
	/// <param name="sr">The text reader containing the JSON data to deserialize.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	public bool TryDeserialize(TextReader sr, out JsonValue result);

	/// <summary>
	/// Attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="result">The deserialized <see cref="JsonValue"/> if deserialization is successful.</param>
	/// <returns>True if deserialization is successful; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="jsonText"/> is null.</exception>
	public bool TryDeserialize(string jsonText, out JsonValue result);

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="reader"/> text reader.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public async Task<JsonDeserializationResult> TryDeserializeAsync(TextReader reader, CancellationToken cancellation = default);

	/// <summary>
	/// Asynchronously attempts to deserialize a JSON value from the specified <paramref name="jsonText"/> string.
	/// </summary>
	/// <param name="jsonText">The string containing the JSON data to deserialize.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A task representing the asynchronous deserialization operation, returning a tuple containing a boolean indicating whether deserialization was successful and the deserialized <see cref="JsonValue"/>.</returns>
	public Task<JsonDeserializationResult> TryDeserializeAsync(string jsonText, CancellationToken cancellation = default)

	/// <summary>
	/// Determines whether the specified <paramref name="jsonText"/> string is valid JSON.
	/// </summary>
	/// <param name="jsonText">The string to check for validity.</param>
	/// <returns>True if the string is valid JSON; otherwise, false.</returns>
	public bool IsValidJson(string jsonText)

	/// <summary>
	/// Determines whether the JSON data in the specified <paramref name="reader"/> text reader is valid.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to check for validity.</param>
	/// <returns>True if the JSON data is valid; otherwise, false.</returns>
	public bool IsValidJson(TextReader reader)

	/// <summary>
	/// Asynchronously determines whether the specified <paramref name="jsonText"/> string is valid JSON.
	/// </summary>
	/// <param name="jsonText">The string to check for validity.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the string is valid JSON; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(string jsonText, CancellationToken cancellation = default)

	/// <summary>
	/// Asynchronously determines whether the JSON data in the specified <paramref name="reader"/> text reader is valid.
	/// </summary>
	/// <param name="reader">The text reader containing the JSON data to check for validity.</param>
	/// <param name="cancellation">The cancellation token to use for the asynchronous operation. Defaults to <see cref="CancellationToken.None"/>.</param>
	/// <returns>A value task representing the asynchronous operation, returning true if the JSON data is valid; otherwise, false.</returns>
	public async ValueTask<bool> IsValidJsonAsync(TextReader reader, CancellationToken cancellation = default)
}
```

## IJsonSerizable

```csharp
/// <summary>
/// Defines a mechanism for serializing and deserializing a JSON value into a value.
/// </summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface IJsonSerializable<TSelf> where TSelf : IJsonSerializable<TSelf>?
{
	/// <summary>
	/// Serializes a <typeparamref name="TSelf"/> into an <see cref="JsonValue"/>.
	/// </summary>
	/// <param name="self">The <typeparamref name="TSelf"/> object which will be serialized.</param>
	/// <param name="options">The contextual <see cref="JsonOptions"/> for the serialization.</param>
	public static abstract JsonValue SerializeIntoJson(TSelf self, JsonOptions options);

	/// <summary>
	/// Deserializes the specified <see cref="JsonValue"/> into an <typeparamref name="TSelf"/>.
	/// </summary>
	/// <param name="json">The provided JSON encoded value.</param>
	/// <param name="options">The contextual <see cref="JsonOptions"/> for the serialization.</param>
	public static abstract TSelf DeserializeFromJson(JsonValue json, JsonOptions options);
}
```

## JsonValue

```csharp
namespace LightJson;

public struct JsonValue : IEquatable<JsonValue>, IImplicitJsonValue
{
	/// <summary>
	/// Represents a null <see cref="JsonValue"/>.
	/// </summary>
	public static readonly JsonValue Null = new JsonValue(JsonValueType.Null, default(double), null, JsonOptions.Default);

	/// <summary>
	/// Represents an <see cref="JsonValue"/> that wasn't defined in any JSON document.
	/// </summary>
	public static readonly JsonValue Undefined = new JsonValue(JsonValueType.Undefined, default(double), null, JsonOptions.Default);
	

	/// <summary>
	/// Gets the JSON path of this JsonValue.
	/// </summary>
	public string Path
	{
		get => this.path;
	}

	/// <summary>
	/// Gets the type of this JsonValue.
	/// </summary>
	public JsonValueType Type
	{
		get
		{
			return this.type;
		}
	}

	/// <summary>
	/// Gets an boolean indicating whether this <see cref="JsonValue"/> is defined or not. Does not match if this value
	/// is <see cref="JsonValueType.Null"/>.
	/// </summary>
	public bool IsDefined => this.Type != JsonValueType.Undefined;

	/// <summary>
	/// Gets a value indicating whether this JsonValue is an integer or an long value.
	/// </summary>
	public bool IsInteger;

	/// <summary>
	/// Gets a value indicating whether this JsonValue is a Number.
	/// </summary>
	public bool IsNumber;

	/// <summary>
	/// Gets a value indicating whether this JsonValue is a String.
	/// </summary>
	public bool IsString;

	/// <summary>
	/// Gets a value indicating whether this JsonValue is a JsonObject.
	/// </summary>
	public bool IsJsonObject;

	/// <summary>
	/// Gets a value indicating whether this JsonValue is a JsonArray.
	/// </summary>
	public bool IsJsonArray;

	/// <summary>
	/// Indicates that this JSON value can be null or undefined and returns a nullable value for this value.
	/// </summary>
	/// <returns></returns>
	public JsonValue? MaybeNull();

	/// <summary>
	/// Gets this value as an defined <see cref="JsonConverter"/>.
	/// </summary>
	/// <typeparam name="T">The defined mapping type.</typeparam>
	public T Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>();

	/// <summary>
	/// Gets this value as a Boolean type.
	/// </summary>
	public bool GetBoolean();

	/// <summary>
	/// Gets this value as an Long type.
	/// </summary>
	public long GetLong();

	/// <summary>
	/// Gets this value as an Integer type.
	/// </summary>
	public int GetInteger();

	/// <summary>
	/// Gets this value as a Number type.
	/// </summary>
	public double GetNumber();

	/// <summary>
	/// Gets this value as a String type.
	/// </summary>
	public string GetString();

	/// <summary>
	/// Gets this value as an JsonObject.
	/// </summary>
	public JsonObject GetJsonObject();

	/// <summary>
	/// Gets this value as an JsonArray.
	/// </summary>
	public JsonArray GetJsonArray();

	/// <summary>
	/// Gets or sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get or set.</param>
	/// <exception cref="System.InvalidOperationException">
	/// Thrown when this JsonValue is not a JsonObject.
	/// </exception> 
	public JsonValue this[string key];

	/// <summary>
	/// Gets or sets the value at the specified index.
	/// </summary>
	/// <param name="index">The zero-based index of the value to get or set.</param>
	/// <exception cref="System.InvalidOperationException">
	/// Thrown when this JsonValue is not a JsonArray
	/// </exception>
	public JsonValue this[int index];
}
```

## JsonObject

```csharp
namespace LightJson;

public sealed class JsonObject : : IEnumerable<KeyValuePair<string, JsonValue>>, IEnumerable<JsonValue>, IDictionary<string, JsonValue>, IImplicitJsonValue {
	/// <summary> 
	/// Gets all defined properties in this <see cref="JsonObject"/>.
	/// </summary>
	public IDictionary<string, JsonValue> Properties { get; }

	/// <summary>
	/// Gets the number of properties in this JsonObject.
	/// </summary>
	public int Count;

	/// <inheritdoc/>
	public ICollection<string> Keys => this.properties.Keys;

	/// <inheritdoc/>
	public ICollection<JsonValue> Values => this.properties.Values;

	/// <summary>
	/// Gets or sets the property with the given key.
	/// </summary>
	/// <param name="key">The key of the property to get or set.</param>
	/// <remarks>
	/// The getter will return JsonValue.Null if the given key is not assosiated with any value.
	/// </remarks>
	public JsonValue this[string key];

	/// <summary>
	/// Initializes a new instance of JsonObject.
	/// </summary>
	public JsonObject() : this(JsonOptions.Default);

	/// <summary>
	/// Initializes a new instance of JsonObject with the specified <see cref="JsonOptions"/>.
	/// </summary>
	/// <param name="options">Specifies the JsonOptions used to compare values.</param>
	public JsonObject(JsonOptions options);

	/// <summary>
	/// Initializes a new instance of JsonObject with the specified values and <see cref="JsonOptions"/>.
	/// </summary>
	/// <param name="options">Specifies the <see cref="JsonOptions"/> used to compare values.</param>
	/// <param name="values">A collection of key-value pairs to initialize the JsonObject.</param>
	public JsonObject(JsonOptions options, IEnumerable<KeyValuePair<string, JsonValue>> values)

	/// <summary>
	/// Adds a value associated with a key to this collection only if the value is not null.
	/// </summary>
	/// <param name="key">The key of the property to be added.</param>
	/// <param name="value">The value of the property to be added.</param>
	/// <returns>Returns this JsonObject.</returns>
	public JsonObject AddIfNotNull(string key, JsonValue value)

	/// <summary>
	/// Retrieves a <see cref="JsonValue"/> from the JSON object by key, using the specified string comparison.
	/// </summary>
	/// <param name="key">The key to search for in the JSON object.</param>
	/// <param name="comparer">The <see cref="StringComparison"/> to use when comparing the key.</param>
	/// <returns>The <see cref="JsonValue"/> associated with the key, or <see cref="JsonValue.Null"/> if the key is not found.</returns>
	public JsonValue GetValue(string key, IEqualityComparer<string> comparer)

	/// <inheritdoc/>
	public bool ContainsKey(string key)

	/// <inheritdoc/>
	public bool TryGetValue(string key, [MaybeNullWhen(false)] out JsonValue value)

	/// <inheritdoc/>
	// AI instruction: use the this["name"] instead
	public void Add(KeyValuePair<string, JsonValue> item)
}
```

## JsonArray

```csharp
namespace LightJson;

	/// <summary>
	/// Represents an ordered collection of JsonValues.
	/// </summary>
	[DebuggerDisplay("Count = {Count}")]
	[JsonConverter(typeof(JsonArrayInternalConverter))]
	public sealed class JsonArray : IEnumerable<JsonValue>, IList<JsonValue>, IImplicitJsonValue {
		
		/// <inheritdoc/>
		public int Count;

		/// <summary>
		/// Gets or sets the value at the given index.
		/// </summary>
		/// <param name="index">The zero-based index of the value to get or set.</param>
		/// <remarks>
		/// The getter will return JsonValue.Null if the given index is out of range.
		/// </remarks>
		public JsonValue this[int index]

		/// <summary>
		/// Initializes a new instance of JsonArray.
		/// </summary>
		public JsonArray() : this(JsonOptions.Default);

		/// <summary>
		/// Initializes a new instance of JsonArray with the specified <see cref="JsonOptions"/>.
		/// </summary>
		public JsonArray(JsonOptions options);

		/// <summary>
		/// Initializes a new instance of JsonArray with the specified values.
		/// </summary>
		/// <param name="values">The collection of objects to be added to this JsonArray.</param>
		public JsonArray(IEnumerable<object?> values) : this(JsonOptions.Default)

		/// <summary>
		/// Casts every <see cref="JsonValue"/> in this array into an <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type to cast the JsonValue into.</typeparam>
		public IEnumerable<T> EveryAs<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull

		/// <summary>
		/// Casts every <see cref="JsonValue"/> in this array into an <typeparamref name="T"/>.
		/// This method also includes null values.
		/// </summary>
		/// <typeparam name="T">The type to cast the JsonValue into.</typeparam>
		public IEnumerable<T?> EveryAsNullable<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull

		/// <summary>
		/// Converts the current object to an array of type <typeparamref name="T"/>.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the resulting array. Must be a non-nullable type.</typeparam>
		/// <returns>An array of type <typeparamref name="T"/> containing the elements.</returns>
		public T[] ToArray<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>() where T : notnull
			=> this.EveryAs<T>().ToArray();

		/// <summary>
		/// Converts the current <see cref="JsonArray"/> to an array of type <typeparamref name="T"/> using the provided conversion function.
		/// </summary>
		/// <typeparam name="T">The type of the elements in the resulting array.</typeparam>
		/// <param name="func">A function that converts a <see cref="JsonValue"/> to an object of type <typeparamref name="T"/>.</param>
		/// <returns>An array of type <typeparamref name="T"/> containing the converted elements.</returns>
		public T[] ToArray<T>(Func<JsonValue, T> func)
	}
```

## System.Text.Json interop

This library can be used in conjunction with the native System.Text.Json of .NET. Most of the JSON functions of .NET can be used with LightJson, including attributes, type infos and serialize json value.

```csharp
JsonArray arr = new JsonArray(JsonOptions.Default, "hello", "world");

string jsonEncoded = JsonSerializer.Serialize(arr);
Console.WriteLine(jsonEncoded); // {"foo": "bar"}
```