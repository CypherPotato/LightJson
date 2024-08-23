using LightJson;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        string userJson = """
            // comments
            { /* real gorilla */
                "nameAndSurname": "foo", // make it panda
                "age" /*international antherm*/: 123 // test
            }
            // test
            """;

        JsonOptions.Default.SerializationFlags = LightJson.Serialization.JsonSerializationFlags.IgnoreComments;
        User n = JsonValue.Deserialize<User>(userJson);

        ;
    }
}

class User
{
    public string NameAndSurname { get; set; }
    public int Age { get; set; }
}