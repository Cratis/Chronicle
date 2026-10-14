```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.EventSources;

public static class DefinitionInspector
{
    public static IEnumerable<string> DescribeStreams(IEventStore eventStore)
    {
        var definition = eventStore.EventSources.GetFor(typeof(AccountEventSource));

        // ConcurrencyFor returns the stream's dimensions when it declares any, otherwise the event source's.
        return definition.Streams.Select(stream => $"{definition.Name}/{stream.Name}: {definition.ConcurrencyFor(stream)}");
    }
}
```
