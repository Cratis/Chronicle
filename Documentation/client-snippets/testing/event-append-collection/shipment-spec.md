```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [Collection(ChronicleCollection.Name)]
    public class and_collecting_the_scheduled_shipment(and_collecting_the_scheduled_shipment.context context)
        : Given<and_collecting_the_scheduled_shipment.context>(context)
    {
#pragma warning disable CS8981 // "context" is a conventional, lowercase BDD nested-class name
        public class context(ChronicleInProcessFixture fixture) : given.a_shipment_reactor_context(fixture)
        {
            async Task Because()
            {
                var reactor = EventStore.Reactors.GetHandlerFor<ShipmentReactor>();
                await reactor.WaitTillActive();

                AppendedEventsCollector = StartCollectingAppends();
                await EventStore.EventLog.Append(EventSourceId, new OrderPlaced("order-123"));

                // Wait for the reactor's follow-up append to arrive
                await AppendedEventsCollector.WaitForCount(2);
            }
        }
#pragma warning restore CS8981

        AppendedEventWithResult Shipment => Context.AppendedEventsCollector.All
            .First(e => e.Event.Content is ShipmentScheduled);

        [Fact] void should_schedule_a_shipment() =>
            Shipment.Event.Content.ShouldBeOfExactType<ShipmentScheduled>();
        [Fact] void should_carry_the_order_id() =>
            ((ShipmentScheduled)Shipment.Event.Content).OrderId.ShouldEqual("order-123");
        [Fact] void should_be_successful() =>
            Shipment.Result.IsSuccess.ShouldBeTrue();
        [Fact] void should_have_a_valid_sequence_number() =>
            Shipment.Result.SequenceNumber.IsActualValue.ShouldBeTrue();
    }
}
```
