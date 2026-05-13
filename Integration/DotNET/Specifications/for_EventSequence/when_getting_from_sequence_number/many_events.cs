// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_getting_from_sequence_number.many_events.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_getting_from_sequence_number;

[Collection(ChronicleCollection.Name)]
public class many_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public IList<SomeEvent> Events { get; private set; }
        public IImmutableList<AppendedEvent> AppendedEvents { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Establish()
        {
            Events = [new SomeEvent("some value"), new SomeEvent("some other value"), new SomeEvent("some third value")];
            await EventStore.EventLog.AppendMany(EventSourceId, Events);
        }

        async Task Because()
        {
            AppendedEvents = await EventStore.EventLog.GetFromSequenceNumber(0);
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber((ulong)Context.Events.Count);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber((ulong)Context.Events.Count - 1);

    [Fact] void should_get_all_the_appended_events() => Context.AppendedEvents.Count.ShouldEqual(3);

    [Fact]
    async Task should_have_stored_all_the_events_in_correct_order()
    {
        foreach (var (e, i) in Context.Events.Select((item, index) => (item, index)))
        {
            await Context.ShouldHaveAppendedEvent<SomeEvent>((ulong)i, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Events[i].Content));
        }
    }
}
