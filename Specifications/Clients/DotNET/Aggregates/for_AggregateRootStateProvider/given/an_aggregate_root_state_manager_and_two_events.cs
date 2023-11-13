// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates.for_AggregateRootStateProvider.given;

public class an_aggregate_root_state_manager_and_two_events : an_aggregate_root_state_manager
{
    protected IEnumerable<AppendedEvent> events;

    void Establish()
    {
        events = new[]
        {
            AppendedEvent.EmptyWithEventType(FirstEventType.EventTypeId),
            AppendedEvent.EmptyWithEventType(SecondEventType.EventTypeId)
        };
    }
}
