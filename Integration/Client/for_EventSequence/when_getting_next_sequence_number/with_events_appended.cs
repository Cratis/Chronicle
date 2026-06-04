// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_getting_next_sequence_number.with_events_appended.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_getting_next_sequence_number;

[Collection(ChronicleCollection.Name)]
public class with_events_appended(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public EventSequenceNumber NextSequenceNumber { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        async Task Establish()
        {
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("first"));
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("second"));
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent("third"));
        }

        async Task Because()
        {
            NextSequenceNumber = await EventStore.EventLog.GetNextSequenceNumber();
        }
    }

    [Fact]
    void should_return_the_sequence_number_after_the_tail() => Context.NextSequenceNumber.ShouldEqual(new EventSequenceNumber(3));
}
