```csharp
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace ExplicitConcurrencyExample
{
    public record OrderPlaced(decimal Total);

    public static class Scope
    {
        public static ConcurrencyScope ForOrder(IEventStore store, EventSourceId source)
        {
            var eventType = store.EventTypes.GetEventTypeFor(typeof(OrderPlaced));
            return new ConcurrencyScopeBuilder()
                .WithEventSourceId(source)
                .WithEventType(eventType)
                .Build();
        }
    }
}
```
