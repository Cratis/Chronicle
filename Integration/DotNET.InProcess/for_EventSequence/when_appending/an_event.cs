// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.an_event.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelEventHash = Cratis.Chronicle.Concepts.Events.EventHash;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class an_event(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }
        public KernelAppendedEvent StoredEvent { get; private set; } = null!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            StoredEvent = await GetEventLogStorage().GetEventAt(EventSequenceNumber.First.Value);
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] Task should_have_the_event_stored() => Context.ShouldHaveAppendedEvent<SomeEvent>(0, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Event.Content));

    [Fact] void should_have_a_hash_set() => Context.StoredEvent.Context.Hash.ShouldNotEqual(KernelEventHash.NotSet);
}
