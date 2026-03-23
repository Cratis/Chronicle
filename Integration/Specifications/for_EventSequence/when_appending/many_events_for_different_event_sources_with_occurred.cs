// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using context = Cratis.Chronicle.InProcess.Integration.for_EventSequence.when_appending.many_events_for_different_event_sources_with_occurred.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_events_for_different_event_sources_with_occurred(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public IList<EventForEventSourceId> Events { get; private set; }
        public DateTimeOffset FirstOccurred { get; private set; }
        public DateTimeOffset SecondOccurred { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            FirstOccurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
            SecondOccurred = new DateTimeOffset(2021, 6, 15, 12, 0, 0, TimeSpan.Zero);
            Events =
            [
                new EventForEventSourceId("source1", new SomeEvent("first"), Causation.Unknown()) { Occurred = FirstOccurred },
                new EventForEventSourceId("source2", new SomeEvent("second"), Causation.Unknown()) { Occurred = SecondOccurred }
            ];
        }

        async Task Because()
        {
            await EventStore.EventLog.AppendMany(Events);
        }
    }

    [Fact]
    async Task should_have_the_correct_occurred_for_first_event()
    {
        var eventLog = Context.GetEventLogStorage();
        var @event = await eventLog.GetEventAt(0);
        @event.Context.Occurred.ShouldEqual(Context.FirstOccurred);
    }

    [Fact]
    async Task should_have_the_correct_occurred_for_second_event()
    {
        var eventLog = Context.GetEventLogStorage();
        var @event = await eventLog.GetEventAt(1);
        @event.Context.Occurred.ShouldEqual(Context.SecondOccurred);
    }
}
