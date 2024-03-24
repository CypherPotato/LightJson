using LightJson;
using LightJson.Converters;
using System.Text.Json;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        JsonOptions.NamingPolicy = JsonNamingPolicy.CamelCase;
        JsonOptions.SerializeFields = true;

        JsonValue val = JsonValue.FromObject(new int[] { 52, 436, 59 });
        Console.WriteLine(val.ToString(true));
    }
}
