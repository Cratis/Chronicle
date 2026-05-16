// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.all_events_for_an_event_source.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelGlobalEventTypes = Cratis.Chronicle.Concepts.Events.GlobalEventTypes;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class all_events_for_an_event_source(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent FirstEvent { get; private set; }
        public SomeEvent SecondEvent { get; private set; }
        public KernelAppendedEvent StoredFirstEvent { get; private set; }
        public KernelAppendedEvent StoredSecondEvent { get; private set; }
        public KernelAppendedEvent SystemStoredEvent { get; private set; }

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
            var storage = GetEventLogStorage();
            StoredFirstEvent = await storage.GetEventAt(EventSequenceNumber.First.Value);
            StoredSecondEvent = await storage.GetEventAt((EventSequenceNumber.First + 1).Value);
            var systemStorage = GetSystemEventLogStorage();
            var tailSequenceNumber = await systemStorage.GetTailSequenceNumber();
            SystemStoredEvent = await systemStorage.GetEventAt(tailSequenceNumber);
        }
    }

    [Fact]
    void should_mark_first_event_as_redacted() => Context.StoredFirstEvent.Context.EventType.Id.Value.ShouldEqual(KernelGlobalEventTypes.Redaction.Value);

    [Fact]
    void should_mark_second_event_as_redacted() => Context.StoredSecondEvent.Context.EventType.Id.Value.ShouldEqual(KernelGlobalEventTypes.Redaction.Value);

    [Fact]
    void should_have_appended_events_redacted_for_event_source_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventsRedactedForEventSource");
}
