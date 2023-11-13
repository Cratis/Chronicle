// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProvider.given;

public class an_aggregate_root_state_manager_and_two_events : an_aggregate_root_state_manager
{
    protected IEnumerable<AppendedEvent> events;
    protected EventType[] event_types = new EventType[]
    {
        FirstEventType.EventTypeId,
        SecondEventType.EventTypeId
    };

    void Establish()
    {
        events = new[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        };

        event_handlers.Setup(_ => _.EventTypes).Returns(event_types.ToImmutableList());
    }
}
