// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting.a_single_event_via_event_sequence.context;
using KernelAppendedEvent = Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelGlobalEventTypes = Cratis.Chronicle.Concepts.Events.GlobalEventTypes;

namespace Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_redacting;

[Collection(ChronicleCollection.Name)]
public class a_single_event_via_event_sequence(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
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
            await EventStore.EventLog.Redact(EventSequenceNumber.First, "test reason");

            // The redaction is performed asynchronously by EventSequencesReactor — poll until it completes.
            var storage = GetEventLogStorage();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            do
            {
                StoredEvent = await storage.GetEventAt(EventSequenceNumber.First.Value);
                if (StoredEvent.Context.EventType.Id != KernelGlobalEventTypes.Redaction)
                {
                    await Task.Delay(50, cts.Token);
                }
            }
            while (StoredEvent.Context.EventType.Id != KernelGlobalEventTypes.Redaction);

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
