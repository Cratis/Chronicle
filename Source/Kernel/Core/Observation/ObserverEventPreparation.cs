// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>Shared job and probe filtering without changing event identity.</summary>
internal static class ObserverEventPreparation
{
    /// <summary>Prepares one bounded batch with the ordinary observer filters.</summary>
    /// <param name="observationState">The delivery mode.</param>
    /// <param name="events">The stored events.</param>
    /// <param name="nonRedactionEventTypeIds">The subscribed non-redaction event types.</param>
    /// <param name="filters">The observer filters.</param>
    /// <returns>The deliverable events in their original order.</returns>
    internal static AppendedEvent[] Prepare(EventObservationState observationState, IEnumerable<AppendedEvent> events, HashSet<EventTypeId> nonRedactionEventTypeIds, ObserverFilters? filters) =>
        filters.Apply(FilterRedactedEventsForUnsubscribedTypes(SetObservationStateIfSpecified(observationState, events), nonRedactionEventTypeIds));

    static AppendedEvent[] SetObservationStateIfSpecified(EventObservationState eventObservationState, IEnumerable<AppendedEvent> events)
    {
        if (eventObservationState != EventObservationState.None)
        {
            return events.Select(@event =>
                @event with
                {
                    Context = @event.Context with
                    {
                        ObservationState = eventObservationState
                    }
                }).ToArray();
        }

        return events.ToArray();
    }

    static AppendedEvent[] FilterRedactedEventsForUnsubscribedTypes(AppendedEvent[] events, HashSet<EventTypeId> nonRedactionEventTypeIds)
    {
        if (nonRedactionEventTypeIds.Count == 0)
        {
            return events;
        }

        var filtered = new AppendedEvent[events.Length];
        var count = 0;
        foreach (var @event in events)
        {
            if (IsSubscribedEvent(@event, nonRedactionEventTypeIds)) filtered[count++] = @event;
        }

        return count == filtered.Length ? filtered : filtered[..count];
    }

    static bool IsSubscribedEvent(AppendedEvent @event, HashSet<EventTypeId> nonRedactionEventTypeIds)
    {
        if (@event.Context.EventType.Id != GlobalEventTypes.Redaction) return true;
        if (@event.Content is not IDictionary<string, object?> contentDict || !contentDict.TryGetValue("originalEventType", out var originalEventTypeObj)) return false;
        var originalEventTypeId = originalEventTypeObj?.ToString();
        return originalEventTypeId is not null && nonRedactionEventTypeIds.Contains(new EventTypeId(originalEventTypeId));
    }
}
