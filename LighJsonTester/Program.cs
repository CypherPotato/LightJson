using LightJson;
using LightJson.Converters;
using LightJson.Serialization;
using System.Text.Json;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        JsonValue obj = new JsonObject()
        {
            { "foo", "bar" },
            { "MyArray", new JsonArray() { 24, 52, 66 } }
        };

        string s = "";
        using (var jw = new JsonWriter())
        {
            jw.UseIndentedSyntax();
            jw.UnquotedPropertyKeys = true;

            jw.NamingPolicy = JsonNamingPolicy.KebabCaseLower;

            jw.Write(obj);
            s = jw.ToString();
        }

        var parsed = JsonValue.Deserialize(s, new JsonOptions() { SerializationFlags = JsonSerializationFlags.All });
        Console.WriteLine(parsed.ToString());
    }
}
