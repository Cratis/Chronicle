```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [Collection(ChronicleCollection.Name)]
    public class and_a_reactor_directly_appends_a_duplicate_unique_value(and_a_reactor_directly_appends_a_duplicate_unique_value.context context)
        : Given<and_a_reactor_directly_appends_a_duplicate_unique_value.context>(context)
    {
#pragma warning disable CS8981 // "context" is a conventional, lowercase BDD nested-class name
        public class context(ChronicleInProcessFixture fixture) : given.a_unique_value_reactor_context(fixture)
        {
            public string UniqueValue = null!;

            async Task Because()
            {
                var reactor = EventStore.Reactors.GetHandlerFor<UniqueValueReactor>();
                await reactor.WaitTillActive();

                UniqueValue = Guid.NewGuid().ToString();
                var firstEventSourceId = EventSourceId.New();
                var secondEventSourceId = EventSourceId.New();

                AppendedEventsCollector = StartCollectingAppends();
                await EventStore.EventLog.Append(firstEventSourceId, new UniqueValueRecorded(UniqueValue));
                await EventStore.EventLog.Append(secondEventSourceId, new UniqueValueRecorded(UniqueValue));
                await AppendedEventsCollector.WaitForCount(4);
            }
        }
#pragma warning restore CS8981

        AppendedEventWithResult ViolatingAppend => Context.AppendedEventsCollector.All
            .First(e => e.Result.HasConstraintViolations);

        [Fact] void should_have_a_constraint_violation() =>
            ViolatingAppend.Result.HasConstraintViolations.ShouldBeTrue();
        [Fact] void should_not_be_successful() =>
            ViolatingAppend.Result.IsSuccess.ShouldBeFalse();
        [Fact] void should_have_attempted_the_follow_up_event() =>
            ViolatingAppend.Event.Content.ShouldBeOfExactType<UniqueValueFollowUp>();
    }
}
```
