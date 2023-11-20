// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Aksio.Cratis.Aggregates.for_ReducerAggregateRootStateProvider.given;

public class an_aggregate_root_that_handles_two_event_types_and_two_appended_events : an_aggregate_root_that_handles_two_event_types
{
    protected IImmutableList<AppendedEvent> events;

    void Establish()
    {
        events = new[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        }.ToImmutableList();
        event_sequence.Setup(_ => _.GetForEventSourceIdAndEventTypes(event_source_id, event_types)).ReturnsAsync(events);
    }
}
