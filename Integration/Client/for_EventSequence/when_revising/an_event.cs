// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_revising.an_event.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_revising;

[Collection(ChronicleCollection.Name)]
public class an_event(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent OriginalEvent { get; private set; }
        public SomeEvent RevisedEvent { get; private set; }
        public AppendedEvent StoredEvent { get; private set; }
        public AppendedEvent SystemStoredEvent { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            OriginalEvent = new SomeEvent("original content");
            RevisedEvent = new SomeEvent("revised content");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, OriginalEvent);
            await this.ReviseEvent(EventSequenceNumber.First, RevisedEvent);
            var eventLogEvents = await EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
            StoredEvent = eventLogEvents[0];
            var systemLog = EventStore.GetEventSequence(EventSequenceId.System);
            var systemTail = await systemLog.GetTailSequenceNumber();
            var systemEvents = await systemLog.GetFromSequenceNumber(systemTail);
            SystemStoredEvent = systemEvents[0];
        }
    }

    [Fact]
    Task should_have_updated_content() => Context.ShouldHaveAppendedEvent<SomeEvent>(EventSequenceNumber.First, Context.EventSourceId, e => e.Content.ShouldEqual(Context.RevisedEvent.Content));

    [Fact]
    void should_have_a_hash_set() => Context.StoredEvent.Context.Hash.ShouldNotEqual(EventHash.NotSet);

    [Fact]
    void should_have_appended_event_revised_to_system_log() => Context.SystemStoredEvent.Context.EventType.Id.Value.ShouldEqual("EventRevised");
}
