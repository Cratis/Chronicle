```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [EventType]
    public record FirstItemAdded(string Name);

    [EventType]
    public record SecondItemAdded(string Name);

    namespace given
    {
        public class a_multiple_events_scope(ChronicleInProcessFixture fixture) : Specification(fixture)
        {
            public EventSourceId EventSourceId = null!;
            public IEventAppendCollection AppendedEventsCollector = null!;

            public override IEnumerable<Type> EventTypes => [typeof(FirstItemAdded), typeof(SecondItemAdded)];

            void Establish() => EventSourceId = EventSourceId.New();

            async Task Because()
            {
                AppendedEventsCollector = StartCollectingAppends();
                await EventStore.EventLog.Append(EventSourceId, new FirstItemAdded("Widget"));
                await EventStore.EventLog.Append(EventSourceId, new SecondItemAdded("Gadget"));
            }

            void Destroy() => AppendedEventsCollector?.Dispose();
        }
    }

    [Collection(ChronicleCollection.Name)]
    public class and_locating_the_second_event(given.a_multiple_events_scope context)
        : Given<given.a_multiple_events_scope>(context)
    {
        AppendedEventWithResult SecondEvent => Context.AppendedEventsCollector.All.First(e => e.Event.Content is SecondItemAdded);

        [Fact] void should_locate_the_second_event() => SecondEvent.Event.Content.ShouldBeOfExactType<SecondItemAdded>();
        [Fact] void should_carry_the_correct_name() => ((SecondItemAdded)SecondEvent.Event.Content).Name.ShouldEqual("Gadget");
    }
}
```
