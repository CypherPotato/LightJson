# LightJson

> This project was based in the awesome work of [LightJson](https://github.com/MarcosLopezC/LightJson), originally made by Marcos Lopez C.

This fork includes some personal tweaks. It is a JSON library focused on not using reflection, where object mapping is made manually and requires mapping to serialize/deserialize typed JSON messages.

Almost everything in this class is inherited from the main project mentioned above, with new features:

- Unlike the original project, a `JsonValue` does not contain any `As[Type]` properties, but has a method for each JSON type, like `GetString()` or even `GetNumber()`, and the main difference is that you cannot get an implicit value of what the `JsonValue` is. For example, you cannot read `JsonValue.GetBoolean()` if the stored value is a string, even if it's value is `"true"` or `0`.
- All functions that return an object converted from a JsonValue, such as `JsonValue.GetString()` for example, do not return nullable values, neither accepts it. You can check for nullable JSON values using `JsonValue.MaybeNull()`. Null values would throw an exception if not used with `MaybeNull()`.
- Added `JsonOptions`, which contains:
    - `PropertyNameComparer`, which sets the default string comparer used for comparing property names and constructor parameters names.
    - `Converters`, which aims to manage JSON converters.
    - `NamingPolicy`, which transforms the property name of a JSON object on the JSON output.
    - `WriteIndented`, which sets whether the JSON serializer should write indentend, pretty formatted, output.
    - `StringEncoder`, which sets the encoder used for escaping strings.
    - `ThrowOnDuplicateObjectKeys`, which sets an boolean indicating where the JSON parser should throw on duplicated object keys.
    - `DynamicObjectMaxDepth`, sets the maximum depth for serializing or deserializing objects.
    - `AllowNumbersAsStrings`, sets whether the JSON deserializer should allow parsing string JSON values into numeric types.
    - `SerializationFlags`, which allows to pass `JsonSerializationFlags` flags to the JSON serializer/deserializer, such as the JSON5 deserializer.
    - `InfinityHandler` which sets the output for the JSON writer when writing double Infinity numbers.
    - `SerializerContext` which allows you to specify an [IJsonTypeInfoResolver](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.serialization.metadata.ijsontypeinforesolver?view=net-9.0) used for serialization and deserialization of dynamic objects.
- Experimental [JSON5](https://github.com/json5/json5) support with `SerializationFlags.All`.
- Undefined values, as it is, values which aren't defined or does not exist in the parent object/array, will come with `JsonValueType.Undefined` type instead of `JsonValueType.Null`.
- This projects targets .NET 6 and above.
- Experimental support for including the JSON value path into error messages.
- Source generation support through `SerializerContext`.

## Serialize and deserialize data

All serialized or deserialized information results in the `JsonValue` structure. From this object, you can manipulate the JSON document.
In the examples below, we will show how serialization and deserialization works.

```cs
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

## JSON converters

Here's an example of an `System.DateTime` converter, which serializes and deserializes values into it:

```cs
static void Main(string[] args)
{
    JsonOptions.Default.Converters.Add(new DateTimeConverter());
    string json = JsonOptions.Default.SerializeJson(DateTime.Now);    
    Console.WriteLine(json);
}

public sealed class DateTimeConverter : JsonConverter
{
	public override Boolean CanSerialize(Type type, JsonOptions currentOptions)
	{
		return type == typeof(DateTime);
	}
	
	public override Object Deserialize(JsonValue value, Type requestedType, JsonOptions currentOptions)
	{
		return DateTime.Parse(value.GetString());
	}
	
	public override JsonValue Serialize(Object value, JsonOptions currentOptions)
	{
		return new JsonValue(t.ToString("s"));
	}
}
```

These types already has converters defined for them:

- Char
- DateOnly
- DateTime
- Enums
- Guid
- IpAddress
- TimeOnly
- TimeSpan
- Uri
- Dictionaries (only classes which inherits `IDictionary` and `IDictionary<string, object?` are supported)

## Fluent syntax for retrieving items

```cs
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

## System.Text.Json interop

This library can be used in conjunction with the native System.Text.Json of .NET. Most of the JSON functions of .NET can be used with LightJson, including attributes, type infos and serialize json value.

```csharp
JsonArray arr = new JsonArray(JsonOptions.Default, "hello", "world");

string jsonEncoded = JsonSerializer.Serialize(arr);
Console.WriteLine(jsonEncoded); // {"foo": "bar"}
```