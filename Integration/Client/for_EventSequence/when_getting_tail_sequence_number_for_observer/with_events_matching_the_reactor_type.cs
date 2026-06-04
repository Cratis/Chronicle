// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_tail_sequence_number_for_observer.with_events_matching_the_reactor_type.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_tail_sequence_number_for_observer;

[Collection(ChronicleCollection.Name)]
public class with_events_matching_the_reactor_type(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public EventSequenceNumber TailSequenceNumber { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(AnotherEvent)];

        async Task Establish()
        {
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("first"));   // seq #0
            await EventStore.EventLog.Append(EventSourceId, new AnotherEvent(42));     // seq #1
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("third"));   // seq #2
        }

        async Task Because()
        {
            TailSequenceNumber = await EventStore.EventLog.GetTailSequenceNumberForObserver(typeof(SomeReactor));
        }
    }

    [Fact]
    void should_return_the_sequence_number_of_the_last_event_handled_by_the_observer() =>
        Context.TailSequenceNumber.ShouldEqual(new EventSequenceNumber(2));
}
