```csharp title="Program.cs"
using var client = new ChronicleClient();
var eventStore = await client.GetEventStore("ChronicleConsole");

await eventStore.EventLog.Append("some-event-source", new TestEvent("Hello world!"));
```
