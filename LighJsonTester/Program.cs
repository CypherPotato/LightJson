using LightJson;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        JsonValue val = JsonValue.Serialize(new { batata = true });

        var n = val.Get<JsonValue>();

        Console.WriteLine(n.ToString());

    }
}

public class PatientOccupationData
{
    public required string Occupation { get; set; }
    public required string Profession { get; set; }
    public int EmploymentYears { get; set; }
    public required string CompanyCNPJ { get; set; }
    public required string CompanyCEP { get; set; }
}