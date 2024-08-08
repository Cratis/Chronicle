// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.OrleansInProcess.for_EventSequence.when_appending;

[Collection(GlobalCollection.Name)]
public class an_event(an_event.context fixture) : OrleansTest<an_event.context>(fixture)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }

        public override Task Establish()
        {
            Event = new SomeEvent("some content");
            return Task.CompletedTask;
        }
        public override async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact]
    Task should_have_correct_next_sequence_number() => Fixture.ShouldHaveCorrectNextSequenceNumber(1);

    [Fact]
    Task should_have_correct_tail_sequence_number() => Fixture.ShouldHaveCorrectTailSequenceNumber(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    Task should_have_the_event_stored() => Fixture.ShouldHaveStoredCorrectEvent<SomeEvent>(0, Fixture.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Fixture.Event.Content));
}
