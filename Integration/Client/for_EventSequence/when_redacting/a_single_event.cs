// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_redacting.a_single_event.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelGlobalEventTypes = Cratis.Chronicle.Concepts.Events.GlobalEventTypes;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class a_single_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }
        public KernelAppendedEvent StoredEvent { get; private set; }
        public KernelAppendedEvent SystemStoredEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            await this.RedactEvent(EventSequenceNumber.First, "test reason");
            StoredEvent = await GetEventLogStorage().GetEventAt(EventSequenceNumber.First.Value);
            var systemStorage = GetSystemEventLogStorage();
            var tailSequenceNumber = await systemStorage.GetTailSequenceNumber();
            SystemStoredEvent = await systemStorage.GetEventAt(tailSequenceNumber);
        }
    }

    [Fact]
    void should_mark_event_as_redacted() => Context.StoredEvent.Context.EventType.Id.Value.ShouldEqual(KernelGlobalEventTypes.Redaction.Value);

    [Fact]
    void should_have_appended_event_redaction_requested_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRedactionRequested");
}
