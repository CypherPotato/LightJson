using LightJson;

namespace LighJsonTester;

internal class Program {
    static void Main ( string [] args ) {
        JsonOptions.Default.WriteIndented = true;
        var json = JsonOptions.Default.Describe ( typeof ( User ) );
        Console.WriteLine ( json.ToString () );
    }
}

class User {
    public string Name { get; set; }
    public int Age { get; set; }
    public Address [] Addresses { get; set; } = Array.Empty<Address> ();
}

struct Address {
    public string Street { get; set; }
    public string City { get; set; }
    public Point Point { get; set; }
}

public readonly record struct Point : IJsonSerializable<Point> {
    public readonly double X;
    public readonly double Y;

    public Point ( double x, double y ) {
        this.X = x;
        this.Y = y;
    }

    public static Point DeserializeFromJson ( JsonValue json, JsonOptions options ) {
        var jarr = json.GetJsonArray ();
        return new Point ( jarr [ 0 ].GetNumber (), jarr [ 1 ].GetNumber () );
    }

    public static JsonValue SerializeIntoJson ( Point self, JsonOptions options ) {
        return new JsonArray ( options ) { self.X, self.Y };
    }
}