// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for working with counts of handled <see cref="AppendedEvent"/> instances.
/// </summary>
public static class HandledEventCountExtensions
{
    /// <summary>
    /// Counts the events, broken down by their <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="events">The events to count.</param>
    /// <returns>The number of events per <see cref="EventTypeId"/>.</returns>
    public static IReadOnlyDictionary<EventTypeId, EventCount> CountByEventType(this IEnumerable<AppendedEvent> events)
    {
        var counts = new Dictionary<EventTypeId, EventCount>();
        foreach (var eventTypeId in events.Select(_ => _.Context.EventType.Id))
        {
            counts[eventTypeId] = counts.GetValueOrDefault(eventTypeId, EventCount.Zero) + 1UL;
        }

        return counts;
    }
}
