using LightJson;
using LightJson.Converters;
using LightJson.Serialization;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LighJsonTester;

class Program
{
    static void Main()
    {
        JsonValue jsonA = JsonOptions.Default.Deserialize("""
            {
                "stringValue": "string",
                "number": "5123",
                "arr": [
                    {
                        "foo": "bar",
                        "kaz": "daz"
                    }
                ]
            }
            """);

        JsonValue jsonB = JsonOptions.Default.Deserialize("""
            {
                "stringValue": "string",
                "number": "5123",
                "arr": [
                    {
                        "foo": "bar",
                        "kaz": "daz",
                        "hehe": true
                    }
                ]
            }
            """);

        var validator = new JsonStructureValidator
        {
            AllowNullValues = false,
            AllowEmptyArrays = false
        };
        var result = validator.StructureEquals(jsonA, jsonB);
        ;
    }
}