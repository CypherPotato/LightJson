# LightJson

This repository contains code that was based on the incredible [LightJson](https://github.com/MarcosLopezC/LightJson) work by Marcos Lopez C. which I made
with a personal touch.

This library is a very simple component for writing and reading JSON messages. It does
not use [Source Generators](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) to read
or write messages, which makes it possible to build code with [bflat](https://github.com/bflattened/bflat) and also
features manual object mapping.

Almost everything in this class is inherited from the main project mentioned above, however, there are these changes:

- Unlike the original project, a `JsonValue` does not contain any `AsFoobar` properties, but has
a method for each `GetFoobar()`, and the main difference is that you cannot get an implicit
value of what the JsonValue is. For example, you cannot read `JsonValue.GetBoolean()` if
the stored value is a string.
- All functions that return an object converted from a JsonValue, such as `JsonValue.GetString()` for example, do
not return nullable values. You can check for nullable JSON values using `JsonValue.MaybeNull()`.
- You can search for objects in an ignore-case manner if you set `JsonOptions.PropertyNameCaseInsensitive` to `true`.
- Undefined values will come with `JsonValueType.Undefined` instead of `JsonValueType.Null`.
- This projects only targets .NET 6 and above.

Additionally, this library includes these new features:

#### Create JsonValues from any kind of object:

```cs
object anonymousObject = new
{
    foo = "bar"
};

string json = new JsonValue(anonymousObject).ToString();
```


#### Create custom mappers (a.k.a. converters):

```cs
static void Main(string[] args)
{
    JsonOptions.Mappers.Add(new DatetimeMapper());

    string json = new JsonValue(DateTime.Now).ToString();

    Console.WriteLine(json);
}

public class DatetimeMapper : JsonSerializerMapper
{
    public override Boolean CanSerialize(Type obj)
    {
        return obj == typeof(DateTime);
    }

    public override Object Deserialize(JsonValue value)
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

#### Fluent syntax for retrieving items:

```cs
// $.foobar must be present, non null and carry an string value.
string stringValue = obj["foobar"].GetString();

// $.bazdaz can be null or undefined, but if not, it must be an string.
string? optionalValue = obj["bazdaz"].MaybeNull()?.GetString();

// $.duzkaz must be present, non null, be an json array and every children on it
// must be an string value.
string[] arrayItems = obj["duzkaz"].GetJsonArray().Select(i => i.GetString()).ToArray();

// $.user must be present, non null, and must be mapped to User, which it's mapper
// is defined on JsonOptions.Mappers.
User user = obj["user"].Map<User>();
```