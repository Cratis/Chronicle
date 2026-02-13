# Cross-cutting properties

You can add properties to all events that are appended without requiring each event type to define the fields. This is useful for metadata such as correlation identifiers, tenant identifiers, or system-generated values that should not be set by domain code.

To provide cross-cutting properties, implement `ICanProvideAdditionalEventInformation`:

```csharp
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

public class MyAdditionalEventInformationProvider : ICanProvideAdditionalEventInformation
{
    public Task ProvideFor(JsonObject @event)
    {
        @event.Add("something", Guid.NewGuid());
        @event.Add("someTime", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
```

Chronicle will discover providers automatically and apply the additional properties during event append.

