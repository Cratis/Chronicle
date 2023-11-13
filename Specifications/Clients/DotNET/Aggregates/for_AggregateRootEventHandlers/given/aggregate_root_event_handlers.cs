// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootEventHandlers.given;

public class aggregate_root_event_handlers : Specification
{
    protected AggregateRootEventHandlers handlers;
    protected Mock<IEventTypes> event_types;
    protected StatelessAggregateRoot aggregate_root;

    void Establish()
    {
        event_types = new();
        event_types.Setup(_ => _.GetEventTypeFor(typeof(FirstEventType))).Returns(FirstEventType.EventTypeId);
        event_types.Setup(_ => _.GetEventTypeFor(typeof(SecondEventType))).Returns(SecondEventType.EventTypeId);

        aggregate_root = new();

        handlers = new AggregateRootEventHandlers(typeof(StatelessAggregateRoot), event_types.Object);
    }
}
