using LightJson;

namespace LighJsonTester;

internal class Program
{
    static void Main(string[] args)
    {
        string userJson = """
            {
              "addressBook": [
                {"lastName": "Average", "firstName": "Joe"},
                {"lastName": "Doe", "firstName": "Jane"},
                {"lastName": "Smith", "firstName": "John"}
              ]
            }
            """;

        var n = JsonValue.Deserialize<BookList>(userJson);

        ;
    }
}

public class BookList
{
    public HashSet<Book> AddressBook { get; set; } = new HashSet<Book>();
}

public class Book
{
    public string? LastName { get; set; }
    public string? FirstName { get; set; }
}