using LightJson;
using LightJson.Converters;
using LightJson.Serialization;
using System.Dynamic;
using System.Text.Json;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        string foo = """
            {
                foo: "baaar"
            }
            """;

        var decoded = JsonValue.Deserialize(foo, new JsonOptions() { SerializationFlags = JsonSerializationFlags.All });

        dynamic faxa = decoded.Get<ExpandoObject>();

        Console.WriteLine(faxa.foo);

        ;
    }
}
