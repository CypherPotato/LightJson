# LightJson

> This project was based in the awesome work of [LightJson](https://github.com/MarcosLopezC/LightJson), originally made by Marcos Lopez C.

This fork includes some personal tweaks. It is a JSON library focused on not using reflection, where object mapping is made manually and requires mapping to serialize/deserialize typed JSON messages.

It also does **not** use [Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to read
or write messages, which makes it possible to build code with [bflat](https://github.com/bflattened/bflat) or AOT-Compilation without source generation.

Almost everything in this class is inherited from the main project mentioned above, with new features:

- Unlike the original project, a `JsonValue` does not contain any `As[Type]` properties, but has a method for each JSON type, like `GetString()` or even `GetNumber()`, and the main difference is that you cannot get an implicit value of what the `JsonValue` is. For example, you cannot read `JsonValue.GetBoolean()` if the stored value is a string, even if it's value is `"true"` or `0`.
- All functions that return an object converted from a JsonValue, such as `JsonValue.GetString()` for example, do not return nullable values. You can check for nullable JSON values using `JsonValue.MaybeNull()`. Null values would throw an exception if not used with `MaybeNull()`.
- Added `JsonOptions`, which contains:
    - `PropertyNameCaseInsensitive`, which indicates whether a property's name uses a case-insensitive comparison when getting values.
    - `SerializeFields`, gets or sets whether the `JsonValue.Serialize` should serialize fields or not.
    - `Converters`, which aims to manage JSON converters.
    - `NamingPolicy`, which transforms the property name of a JSON object on the JSON output.
    - `WriteIndented`, which sets whether the JSON serializer should write indentend, pretty formatted, output.
    - `SerializationFlags`, which allows to pass `JsonSerializationFlags` flags to the JSON serializer/deserializer.
    - `ThrowOnDuplicateObjectKeys` which allows to throw an exception on duplicate key names. This property was enabled by defaut in order versions and original project fork, but disabled since v0.9.
- Experimental [JSON5](https://github.com/json5/json5) support with `SerializationFlags.All`.
- Undefined values, as it is, values which aren't defined or does not exist in the parent object/array, will come with `JsonValueType.Undefined` type instead of `JsonValueType.Null`.
- This projects targets .NET 6 and above.
- Experimental support for including the JSON value path into error messages.

## Serialize and deserialize data

All serialized or deserialized information results in the `JsonValue` structure. From this object, you can manipulate the JSON document.
In the examples below, we will show how serialization and deserialization works.

```cs
// serialize primitive values
json = new JsonValue("hello").ToString();
Console.WriteLine(json); // "hello"

// serialize complex objects
json = JsonValue.Serialize(new { prop1 = "hello", prop2 = "world" }).ToString();
Console.WriteLine(json); // {"prop1":"hello","prop2":"world"}

// for custom types, an converter must be defined in the JsonOptions.Converters
json = JsonValue.Serialize(Guid.NewGuid(), new JsonOptions()).ToString();
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

var objJson = JsonValue.Deserialize(json);
        
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
    JsonOptions.Default.Converters.Add(new DatetimeMapper());

    string json = JsonValue.Serialize(DateTime.Now).ToString();

    Console.WriteLine(json);
}

public class DatetimeMapper : JsonConverter
{
    public override Boolean CanSerialize(Type obj)
    {
        return obj == typeof(DateTime);
    }

    public override Object Deserialize(JsonValue value, Type requestedType)
    {
        return DateTime.Parse(value.GetString());
    }

    public override JsonValue Serialize(Object value)
    {
        DateTime t = (DateTime)value;
        return new JsonValue(t.ToString("s"));
    }
}
```

Also, these converters are defined by default:

- **DictionaryConverter**, which converts `IDictionary<string, object?>` into an JsonObject, and vice-versa.
- **GuidConverter**, which converts `System.Guid` into an string, and vice-versa.
- **EnumConverter** which converts an enum value into it's string representation, and vice versa, enabled through `EnumConverter.EnumToString`.
- **DatetimeConverter** which converts `System.DateTime` into string, and vice-versa, using the format `DatetimeConverter.Format`.
- **DateOnlyConverter** which converts `System.DateOnly` into string, and vice-versa, using the format `DateOnlyConverter.Format`.
- **TimeOnlyConverter** which converts `System.TimeOnly` into string, and vice-versa.
- **TimeSpanConverter** which converts `System.TimeSpan` into string, and vice-versa.
- **CharConverter** which converts `System.Char` into string, and vice-versa.
- **DecimalConverter** which converts `System.Decimal` into an double number, and vice-versa.

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

var obj = JsonValue.Deserialize(json);

// $.foobar must be present, non null and carry an string value.
string stringValue = obj["foobar"].GetString();

// $.bazdaz can be null or undefined, but if not, it must be an string.
string? optionalValue = obj["bazdaz"].MaybeNull()?.GetString();

// $.duzkaz must be present, non null, be an json array and every children on it
// must be an string value.
string[] arrayItems = obj["duzkaz"].GetJsonArray().Select(i => i.GetString()).ToArray();

// $.user must be present, non null, and must be converted to the User type, which it's converter
// is defined on JsonOptions.Converters.
User user = obj["user"].Get<User>();
```