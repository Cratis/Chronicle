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
            Event = new SomeEvent();
            return Task.CompletedTask;
        }
        public override async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact]
    async Task should_have_correct_tail_sequence_number_stored()
    {
        var eventLog = Fixture.GetEventLogStorage();
        var number = await eventLog.GetTailSequenceNumber();
        number.ShouldEqual(Concepts.Events.EventSequenceNumber.First);
    }

    [Fact]
    async Task should_have_events_for_the_event_source_stored()
    {
        var eventLog = Fixture.GetEventLogStorage();
        var hasEvents = await eventLog.HasEventsFor(new Concepts.Events.EventSourceId(Fixture.EventSourceId.Value));
        hasEvents.ShouldEqual(true);
    }

    [Fact]
    async Task should_have_the_event_stored()
    {
        var eventLog = Fixture.GetEventLogStorage();
        var evt = await eventLog.GetEventAt(0);
        var eventType = Fixture.EventStore.EventTypes.GetEventTypeFor(Fixture.Event.GetType());
        evt.Context.EventSourceId.Value.ShouldEqual(Fixture.EventSourceId.Value);
        evt.Metadata.SequenceNumber.ShouldEqual(Concepts.Events.EventSequenceNumber.First);
        evt.Metadata.Type.Id.Value.ShouldEqual(eventType.Id.Value);
    }
}
