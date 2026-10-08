// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverFilters.given;

public static class an_event
{
    public static AppendedEvent With(IEnumerable<Tag>? tags = null, EventSourceType? eventSourceType = null, EventStreamType? eventStreamType = null) =>
        AppendedEvent.EmptyWithEventSequenceNumber(1UL) with
        {
            Context = EventContext.Empty with
            {
                SequenceNumber = 1UL,
                Tags = tags ?? [],
                EventSourceType = eventSourceType ?? EventSourceType.Default,
                EventStreamType = eventStreamType ?? EventStreamType.All
            }
        };
}
