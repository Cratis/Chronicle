// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.all_events_for_an_event_source.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class all_events_for_an_event_source(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent FirstEvent { get; private set; }
        public SomeEvent SecondEvent { get; private set; }
        public AppendedEvent StoredFirstEvent { get; private set; }
        public AppendedEvent StoredSecondEvent { get; private set; }
        public AppendedEvent SystemStoredEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            FirstEvent = new SomeEvent("first content");
            SecondEvent = new SomeEvent("second content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, FirstEvent);
            await EventStore.EventLog.Append(EventSourceId, SecondEvent);
            await this.RedactEventsForEventSource(EventSourceId, "test reason");

            var firstEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            StoredFirstEvent = firstEvents.First();
            var secondEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First + 1);
            StoredSecondEvent = secondEvents.First();

            var systemLog = EventStore.GetEventSequence(EventSequenceId.System);
            var systemTail = await systemLog.GetTailSequenceNumber();
            var systemEvents = await systemLog.GetFromSequenceNumber(systemTail);
            SystemStoredEvent = systemEvents.First();
        }
    }

    [Fact]
    void should_mark_first_event_as_redacted() => Context.StoredFirstEvent.Context.EventType.Id.Value.ShouldEqual("EventRedacted");

    [Fact]
    void should_mark_second_event_as_redacted() => Context.StoredSecondEvent.Context.EventType.Id.Value.ShouldEqual("EventRedacted");

    [Fact]
    void should_have_appended_events_redacted_for_event_source_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventsRedactedForEventSource");
}
