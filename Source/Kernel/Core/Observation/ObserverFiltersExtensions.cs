// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Extension methods for applying <see cref="ObserverFilters"/> to events read for an observer.
/// </summary>
public static class ObserverFiltersExtensions
{
    /// <summary>
    /// Keep only the events that pass the observer's filters.
    /// </summary>
    /// <param name="filters">The <see cref="ObserverFilters"/> to apply, if any.</param>
    /// <param name="events">The events to filter.</param>
    /// <returns>The events that pass the filters, in their original order.</returns>
    /// <remarks>
    /// Uses <see cref="ObserverFilters.Matches(AppendedEvent)"/>, the same check live delivery applies, so an
    /// observer receives the same events whether they arrive live or are read back from the event sequence.
    /// </remarks>
    public static AppendedEvent[] Apply(this ObserverFilters? filters, AppendedEvent[] events)
    {
        if (filters is null || events.Length == 0)
        {
            return events;
        }

        var matching = new AppendedEvent[events.Length];
        var count = 0;
        foreach (var @event in events)
        {
            if (filters.Matches(@event))
            {
                matching[count++] = @event;
            }
        }

        return count == matching.Length ? matching : matching[..count];
    }
}
