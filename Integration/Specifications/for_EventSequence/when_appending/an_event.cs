// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending.an_event.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class an_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);

    [Fact] Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

    [Fact] Task should_have_the_event_stored() => Context.ShouldHaveAppendedEvent<SomeEvent>(0, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Event.Content));
}
