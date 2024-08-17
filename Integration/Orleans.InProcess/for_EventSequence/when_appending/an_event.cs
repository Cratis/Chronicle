// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class an_event(an_event.context context) : OrleansTest<an_event.context>(context)
{
    public class context(GlobalFixture globalFixture) : IntegrationSpecificationContext(globalFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
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

    [Fact]
    Task should_have_correct_next_sequence_number() => Context.ShouldHaveCorrectNextSequenceNumber(1);

    [Fact]
    Task should_have_correct_tail_sequence_number() => Context.ShouldHaveCorrectTailSequenceNumber(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    Task should_have_the_event_stored() => Context.ShouldHaveStoredCorrectEvent<SomeEvent>(0, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Event.Content));
}
