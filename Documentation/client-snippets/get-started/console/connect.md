```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Connections;

public static class GetStartedConsoleClient
{
    public static async Task Run()
    {
        // ChronicleConnectionString.Development points at the local dev kernel on chronicle://localhost:35000
        using var client = new ChronicleClient(ChronicleConnectionString.Development);
        var eventStore = await client.GetEventStore("Quickstart");
        Console.WriteLine($"Connected to event store: {eventStore.Name}");

        // Use eventStore for the lifetime of your program — appending, querying, and so on.
    }
}
```
