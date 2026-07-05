```csharp
namespace Cratis.Chronicle.Docs.EventAppendCollection
{
    [Collection(ChronicleCollection.Name)]
    public class and_collecting_the_registered_item(given.an_event_append_collection_scope context)
        : Given<given.an_event_append_collection_scope>(context)
    {
        [Fact] void should_collect_one_event() => Context.AppendedEventsCollector.All.Count.ShouldEqual(1);
        [Fact] void should_have_appended_the_event() => Context.AppendedEventsCollector.All[0].Event.Content.ShouldBeOfExactType<ItemRegistered>();
        [Fact] void should_be_successful() => Context.AppendedEventsCollector.All[0].Result.IsSuccess.ShouldBeTrue();
        [Fact] void should_have_a_valid_sequence_number() => Context.AppendedEventsCollector.All[0].Result.SequenceNumber.IsActualValue.ShouldBeTrue();
    }
}
```
