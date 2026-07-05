```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    // One-time test project setup: a collection definition sharing one ChronicleInProcessFixture
    // across test classes, and a project-local Specification convenience base.
    [CollectionDefinition(Name)]
    public class ChronicleCollection : ICollectionFixture<ChronicleInProcessFixture>
    {
        public const string Name = "Chronicle";
    }

    public class Specification(ChronicleInProcessFixture fixture) : Specification<ChronicleInProcessFixture>(fixture)
    {
        public override bool AutoDiscoverArtifacts => false;
    }

    [EventType]
    public record ItemRegistered(string Name);

    namespace given
    {
        public class an_event_append_collection_scope(ChronicleInProcessFixture fixture) : Specification(fixture)
        {
            public EventSourceId EventSourceId = null!;
            public IEventAppendCollection AppendedEventsCollector = null!;

            public override IEnumerable<Type> EventTypes => [typeof(ItemRegistered)];

            void Establish() => EventSourceId = EventSourceId.New();

            async Task Because()
            {
                AppendedEventsCollector = StartCollectingAppends();
                await EventStore.EventLog.Append(EventSourceId, new ItemRegistered("Widget"));
            }

            void Destroy() => AppendedEventsCollector?.Dispose();
        }
    }
}
```
