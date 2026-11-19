```csharp
public static class MinimalChronicleConsole
{
    public static async Task Run()
    {
        using var client = new ChronicleClient();
        var eventStore = await client.GetEventStore("my-store");
        await eventStore.Connection.Connect();
    }
}
```
