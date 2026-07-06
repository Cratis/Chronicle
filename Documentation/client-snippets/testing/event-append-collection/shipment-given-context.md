```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    namespace given
    {
        public class a_shipment_reactor_context(ChronicleInProcessFixture fixture) : Specification(fixture)
        {
            public EventSourceId EventSourceId = null!;
            public IEventAppendCollection AppendedEventsCollector = null!;

            public override IEnumerable<Type> EventTypes => [typeof(OrderPlaced), typeof(ShipmentScheduled)];
            public override IEnumerable<Type> Reactors => [typeof(ShipmentReactor)];

            protected override void ConfigureServices(IServiceCollection services) =>
                services.AddSingleton<ShipmentReactor>();

            void Establish() => EventSourceId = EventSourceId.New();

            void Destroy() => AppendedEventsCollector?.Dispose();
        }
    }
}
```
