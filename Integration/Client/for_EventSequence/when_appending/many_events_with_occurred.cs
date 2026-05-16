// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_appending.many_events_with_occurred.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class many_events_with_occurred(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification<ChronicleFixture>(chronicleInProcessFixture)
    {
        public Events.EventSourceId EventSourceId { get; } = "source";
        public IList<SomeEvent> Events { get; private set; }
        public DateTimeOffset Occurred { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Events = [new SomeEvent("some value"), new SomeEvent("some other value"), new SomeEvent("some third value")];
            Occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        async Task Because()
        {
            await EventStore.EventLog.AppendMany(EventSourceId, Events, occurred: Occurred);
        }
    }

    [Fact]
    async Task should_have_the_correct_occurred_for_all_events()
    {
        var events = await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        foreach (var appendedEvent in events)
        {
            appendedEvent.Context.Occurred.ShouldEqual(Context.Occurred);
        }
    }
}
