// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.many_events.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelEventHash = Cratis.Chronicle.Concepts.Events.EventHash;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_events(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public IList<SomeEvent> Events { get; private set; }
        public KernelAppendedEvent FirstStoredEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Events = [new SomeEvent("some value"), new SomeEvent("some other value"), new SomeEvent("some third value")];
        }

        async Task Because()
        {
            await EventStore.EventLog.AppendMany(EventSourceId, Events);
            FirstStoredEvent = await GetEventLogStorage().GetEventAt(EventSequenceNumber.First.Value);
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber((ulong)Context.Events.Count);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber((ulong)Context.Events.Count - 1);

    [Fact]
    async Task should_have_stored_all_the_events_in_correct_order()
    {
        foreach (var (e, i) in Context.Events.Select((item, index) => (item, index)))
        {
            await Context.ShouldHaveAppendedEvent<SomeEvent>((ulong)i, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Events[i].Content));
        }
    }

    [Fact] void should_have_a_hash_set() => Context.FirstStoredEvent.Context.Hash.ShouldNotEqual(KernelEventHash.NotSet);
}
