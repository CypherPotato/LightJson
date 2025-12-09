# LightJson

> This project is a fork of the excellent [LightJson](https://github.com/MarcosLopezC/LightJson) library by Marcos Lopez C.

LightJson is a lightweight, versatile JSON library for .NET, designed with a focus on manual mapping and minimal reflection overhead. This fork significantly expands the original capabilities, introducing modern features such as JSON5 support, JSON Schema validation, TOON serialization, and seamless integration with `System.Text.Json`.

## Key Features

- **Explicit Typing**: `JsonValue` provides explicit methods like `GetString()` or `GetNumber()`. Implicit conversions are avoided to ensure type safety.
- **Null Safety**: Accessor methods do not return nulls by default. Use `MaybeNull()` to safely handle potential null values.
- **Advanced Options**: `JsonOptions` allows extensive customization, including naming policies, indentation, and custom converters.
- **JSON5 Support**: Full support for the [JSON5](https://github.com/json5/json5) standard, including comments, trailing commas, and unquoted keys.
- **JSON Schema**: Built-in support for creating and validating JSON Schemas.
- **TOON Serialization**: Support for Token-Oriented Object Notation (TOON).
- **System.Text.Json Compatibility**: High compatibility with .NET's native JSON library, including support for attributes and `JsonTypeInfo`.
- **Functional Extensions**: Methods like `TryEvaluate`, `TryEvaluateFirst`, and `TryGet` for functional-style JSON manipulation.

## Serialize and Deserialize

All serialized or deserialized data is represented by the `JsonValue` structure, which serves as the entry point for manipulating JSON documents.

```csharp
// Serialize primitive values
string json = new JsonValue("hello").ToString();
Console.WriteLine(json); // "hello"

// Serialize complex objects
json = JsonOptions.Default.Serialize(new { prop1 = "hello", prop2 = "world" }).ToString();
Console.WriteLine(json); // {"prop1":"hello","prop2":"world"}

// Deserialize
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

// Access values
double objNumber = objJson["number"].GetNumber();
string? name = objJson["name"].MaybeNull()?.GetString();
int intAtIndex1 = objJson["arrayOfInts"][1].GetInteger();
Guid convertedValue = objJson["object"]["guid"].Get<Guid>();
```

## Functional Features

`JsonValue` includes functional methods to simplify safe data retrieval and manipulation.

### TryEvaluate & TryEvaluateFirst

Safely evaluate functions on a `JsonValue`, returning a default value if an exception occurs or the value is invalid.

```csharp
// TryEvaluate: Returns "default" if GetString() fails
var value = json["prop"].TryEvaluate(v => v.GetString(), "default");

// TryEvaluateFirst: Tries multiple functions in order
var result = json["prop"].TryEvaluateFirst(new Func<JsonValue, string?>[] {
    v => v.GetString(),
    v => v.GetInteger().ToString()
}, "default");
```

### TryGet

Safely attempts to retrieve a value, converting it to the specified type.

```csharp
// Try to get an integer
if (json["age"].TryGet<int>(out var age))
{
    Console.WriteLine($"Age is {age}");
}

// Try to get by key
var name = json.TryGet<string>("name");
```

## JSON Schema

LightJson supports creating and validating JSON Schemas to ensure data integrity.

```csharp
using LightJson.Schema;

// Create a schema
var schema = JsonSchema.CreateStringSchema(minLength: 5);

// Validate
var result = schema.Validate(new JsonValue("hello"));
if (result.IsValid)
{
    Console.WriteLine("Valid!");
}
```

## JSON5 Support

Enable JSON5 features such as comments, single quotes, and unquoted keys using `JsonSerializationFlags`.

```csharp
var options = new JsonOptions { SerializationFlags = JsonSerializationFlags.Json5 };

var json5 = """
    {
        key: 'value', // Single quotes and unquoted keys
        /* Multi-line
           comment */
        list: [1, 2, 3,] // Trailing comma
    }
    """;

var value = options.Deserialize(json5);
```

## TOON Serialization

Support for TOON (Token-Oriented Object Notation), a format designed for specific serialization needs.

```csharp
using LightJson.Serialization;

using var writer = new StringWriter();
using var toonWriter = new JsonToonWriter(writer);

toonWriter.Write(jsonValue);
var toonOutput = writer.ToString();
```

## System.Text.Json Compatibility

LightJson is designed to work seamlessly with `System.Text.Json`. Types like `JsonValue`, `JsonObject`, and `JsonArray` are decorated with `[JsonConverter]` attributes, allowing them to be directly handled by `System.Text.Json.JsonSerializer`.

```csharp
using System.Text.Json;

var val = new JsonValue("Test");

// Serialize using System.Text.Json
var json = JsonSerializer.Serialize(val); // "Test"

// Deserialize using System.Text.Json
var deserialized = JsonSerializer.Deserialize<JsonValue>(json);
```

It also supports `IJsonTypeInfoResolver` via `JsonOptions.SerializerContext` for advanced scenarios.

## JSON Converters

Custom converters can be registered in `JsonOptions.Converters` to handle specific types.

```csharp
JsonOptions.Default.Converters.Add(new DateTimeConverter());
```

The library includes built-in converters for:
- Char, DateOnly, DateTime, TimeOnly, TimeSpan
- Enum, Guid, IpAddress, Uri
- Dictionaries
