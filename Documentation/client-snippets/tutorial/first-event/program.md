```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

public static class TutorialFirstEventProgram
{
    public static async Task Main()
    {
        using var client = new ChronicleClient(ChronicleConnectionString.Development);
        var eventStore = await client.GetEventStore("LibraryTutorial");
        var bookId = await TutorialFirstEventAppend.AddBook(eventStore);
        Console.WriteLine($"Appended BookAdded for {bookId}");
    }
}
```
