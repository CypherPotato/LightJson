# LightJson

> This project was based in the awesome work of [LightJson](https://github.com/MarcosLopezC/LightJson), originally made by Marcos Lopez C.

This fork includes some personal tweaks. It is a JSON library focused on not using reflection, where object mapping is made manually and does not requires defining types to serialize/deserialize JSON messages.

It also does **not** use [Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to read
or write messages, which makes it possible to build code with [bflat](https://github.com/bflattened/bflat) or AOT-Compilation.

Almost everything in this class is inherited from the main project mentioned above, with new features:

- Unlike the original project, a `JsonValue` does not contain any `AsType` properties, but has a method for each JSON type, like `GetString()` or even `GetNumber()`, and the main difference is that you cannot get an implicit value of what the `JsonValue` is. For example, you cannot read `JsonValue.GetBoolean()` if the stored value is a string, even if it's value is `"true"` or `0`.
- All functions that return an object converted from a JsonValue, such as `JsonValue.GetString()` for example, do not return nullable values. You can check for nullable JSON values using `JsonValue.MaybeNull()`. Null values would throw an exception if not used with `MaybeNull()`.
- Added `JsonOptions` for the parser, which has:
    - `PropertyNameCaseInsensitive`, which indicates whether a property's name uses a case-insensitive comparison when getting values.
    - `SerializeFields`, Gets or sets whether the `JsonValue.FromObject` should serialize fields or not.
    - `Converters`, which aims to manage JSON converters.
    - `NamingPolicy`, which transforms the property name of a JSON object on the JSON output.
- Undefined values, as it is, values which aren't defined or does not exist in the parent object/array, will come with `JsonValueType.Undefined` type instead of `JsonValueType.Null`.
- This projects only targets .NET 6 and above.
- Experimental support for including the JSON value path into error messages.

Additionally, this library includes these new features:

## Serializing objects

You can serialize anonymous objects

```cs
object anonymousObject = new
{
    foo = "bar"
};

string json = JsonValue.FromObject(anonymousObject).ToString();
// {"foo":"bar"}
```

And objects which has their `JsonConverter` defined:

```cs
string json = JsonValue.FromObject(DateTime.Now).ToString();
```

Also, for primitive data-types, you can serialize objects with the `JsonValue` constructors. You cannot implicitly create objects in this project, as the original project does.

```cs
// ❌ error
JsonValue stringValue = "hello";

// ✅ ok
JsonValue stringValue = new JsonValue("hello");
```


## JSON converters

Here's an example of an `System.DateTime` converter, which serializes and deserializes values into it:

```cs
static void Main(string[] args)
{
    JsonOptions.Mappers.Add(new DatetimeMapper());

    string json = JsonValue.FromObject(DateTime.Now).ToString();

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

var obj = JsonValue.Parse(json);

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