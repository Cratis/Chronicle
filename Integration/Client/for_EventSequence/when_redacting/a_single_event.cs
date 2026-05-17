// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.a_single_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class a_single_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }
        public AppendedEvent StoredEvent { get; private set; }
        public AppendedEvent SystemStoredEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            await this.RedactEvent(EventSequenceNumber.First, "test reason");
            var eventLogEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            StoredEvent = eventLogEvents.First();
            var systemLog = EventStore.GetEventSequence(EventSequenceId.System);
            var systemTail = await systemLog.GetTailSequenceNumber();
            var systemEvents = await systemLog.GetFromSequenceNumber(systemTail);
            SystemStoredEvent = systemEvents.First();
        }
    }

    [Fact]
    void should_mark_event_as_redacted() => Context.StoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRedacted");

    [Fact]
    void should_have_appended_event_redaction_requested_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRedactionRequested");
}
