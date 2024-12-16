using LightJson;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        string json = """
            [
                [10, 20],
                [20, 30],
                [40, 50]
            ]
            """;

        JsonOptions.Default.DynamicSerialization = DynamicSerializationMode.Both;

        var points = JsonOptions.Default.Deserialize(json)
            .GetJsonArray().EveryAs<Point>();

        foreach (var point in points)
        {
            Console.WriteLine(point); 
        }
    }
}

public readonly record struct Point : IJsonSerializable<Point>
{
    public readonly double X;
    public readonly double Y;

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static Point DeserializeFromJson(JsonValue json, JsonOptions options)
    {
        var jarr = json.GetJsonArray();
        return new Point(jarr[0].GetNumber(), jarr[1].GetNumber());
    }

    public static JsonValue SerializeIntoJson(Point self, JsonOptions options)
    {
        return new JsonArray(options) { self.X, self.Y };
    }
}