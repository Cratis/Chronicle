```csharp
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;

public class CrossCuttingAdditionalEventInformationProvider : ICanProvideAdditionalEventInformation
{
    public Task ProvideFor(JsonObject @event)
    {
        @event.Add("something", Guid.NewGuid());
        @event.Add("someTime", DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
```
