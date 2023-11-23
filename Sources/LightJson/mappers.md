# LightJson mappers

Use Mappers to convert various objects to JsonValues and vice versa. Example:

```cs
static void Main(string[] args)
{
    string value = """
        {
            "point": { "x": 123, "y": 412 }
        }
        """;

    JsonOptions.Mappers.Add(new PointMapper());
    JsonOptions.PropertyNameCaseInsensitive = true;

    Point point = JsonValue.Parse(value)["point"].As<Point>();
    Console.WriteLine(point);

    // Point { X = 123, Y = 412 }
}

public class PointMapper : JsonSerializerMapper
{
    public override Boolean CanSerialize(Object obj)
    {
        return obj is Point;
    }

    public override Boolean CanDeserialize(JsonValue value)
    {
        return value["X"].IsNumber && value["Y"].IsNumber;
    }

    public override Object Deserialize(JsonValue value)
    {
        return new Point()
        {
            X = (int)value["X"].AsNumber,
            Y = (int)value["Y"].AsNumber
        };
    }

    public override JsonValue Serialize(Object value)
    {
        Point p = (Point)value;
        return new JsonObject
        {
            ["X"] = p.X,
            ["Y"] = p.Y
        };
    }
}

public record struct Point
{
    public int X;
    public int Y;
}
```

You can also for single values, like DateTime:

```cs
public class DatetimeMapper : JsonSerializerMapper
{
    public override Boolean CanDeserialize(JsonValue value)
    {
        return DateTime.TryParse(value.AsString, out _);
    }

    public override Boolean CanSerialize(Object obj)
    {
        return obj is DateTime;
    }

    public override Object Deserialize(JsonValue value)
    {
        return DateTime.Parse(value.AsString!);
    }

    public override JsonValue Serialize(Object value)
    {
        DateTime t = (DateTime)value;
        return t.ToString("G");
    }
}
```