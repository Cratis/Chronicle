// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending.an_event_with_occurred.context;

namespace Cratis.Chronicle.Integration.Specifications.for_EventSequence.when_appending;

[Collection(ChronicleCollection.Name)]
public class an_event_with_occurred(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification<ChronicleFixture>(chronicleInProcessFixture)
    {
        public EventSourceId EventSourceId { get; } = "source";
        public SomeEvent Event { get; private set; }
        public DateTimeOffset Occurred { get; private set; }

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];

        void Establish()
        {
            Event = new SomeEvent("some content");
            Occurred = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event, occurred: Occurred);
        }
    }

    [Fact] Task should_have_the_event_stored() => Context.ShouldHaveAppendedEvent<SomeEvent>(0, Context.EventSourceId.Value, (someEvent) => someEvent.Content.ShouldEqual(Context.Event.Content));

    [Fact]
    async Task should_have_the_correct_occurred()
    {
        var events = await Context.EventStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First);
        events.First().Context.Occurred.ShouldEqual(Context.Occurred);
    }
}
