// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation.and_generation_2_content_is_backfilled.context;

namespace Cratis.Chronicle.Integration.for_EventSequence.when_migrating_existing_events_after_new_generation;

[Collection(ChronicleCollection.Name)]
public class and_generation_2_content_is_backfilled(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        public override IEnumerable<Type> EventTypes => [typeof(OrderPlacedV1), typeof(OrderPlaced)];
        public override IEnumerable<Type> EventTypeMigrators => [typeof(OrderPlacedMigrator)];

        public EventSourceId EventSourceId { get; } = "some-order";
        public OrderPlacedV1 Event { get; private set; }

        void Establish()
        {
            Event = new OrderPlacedV1("Widget order");
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
        }
    }

    [Fact] Task should_have_event_with_description() => Context.ShouldHaveAppendedEvent<OrderPlaced>(0, Context.EventSourceId.Value, e => e.Description.ShouldEqual("Widget order"));
    [Fact] Task should_have_event_with_default_status() => Context.ShouldHaveAppendedEvent<OrderPlaced>(0, Context.EventSourceId.Value, e => e.Status.ShouldEqual("pending"));
}
