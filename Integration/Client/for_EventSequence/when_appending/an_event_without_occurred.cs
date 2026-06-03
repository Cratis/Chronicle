// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.an_event_without_occurred.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class an_event_without_occurred(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification<ChronicleFixture>(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }
        public DateTimeOffset BeforeAppend { get; private set; }
        public DateTimeOffset AfterAppend { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
        }

        async Task Because()
        {
            BeforeAppend = DateTimeOffset.UtcNow;
            await EventStore.EventLog.Append(EventSourceId, Event);
            AfterAppend = DateTimeOffset.UtcNow;
        }
    }

    [Fact]
    async Task should_have_occurred_set_by_server_to_approximately_now()
    {
        var events = await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        events[0].Context.Occurred.ShouldBeInRange(Context.BeforeAppend, Context.AfterAppend);
    }
}
