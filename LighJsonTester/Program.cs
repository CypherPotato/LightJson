using System.Text.Json;
using System.Text.Json.Serialization;
using LightJson;
using LightJson.Serialization;
using Microsoft.Diagnostics.Tracing.Parsers.AspNet;
using static LighJsonTester.Program;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        JsonOptions.Default.SerializerContext = new JsonOptionsSerializerContext(AppJsonContext.Default, new JsonSerializerOptions());

        var json = """
            { 
                "__id": 12,
                "name": "foo"
            }
            """;

        UserRecord user = JsonOptions.Default.Deserialize<UserRecord>(json);
        ;
    }

    public record UserRecord(int id, string? name = null);
}

[JsonSerializable(typeof(UserRecord))]
partial class AppJsonContext : JsonSerializerContext
{
}