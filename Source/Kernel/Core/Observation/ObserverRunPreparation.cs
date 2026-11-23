// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation;

/// <summary>Prepares a partition run without buffering array runs that cannot match its subscription.</summary>
internal static class ObserverRunPreparation
{
    /// <summary>Rejects unsubscribed array runs cheaply, then collects applicable data in a fused traversal.</summary>
    /// <param name="source">The nonempty partition run.</param>
    /// <param name="context">The observer's stable preparation inputs for this turn.</param>
    /// <returns>The original retained event instances and the run-wide decision.</returns>
    /// <exception cref="ArgumentNullException">The source is null.</exception>
    /// <exception cref="InvalidOperationException">The run has no events.</exception>
    internal static PreparedObserverRun Prepare(IEnumerable<AppendedEvent> source, in ObserverRunPreparationContext context)
    {
        ArgumentNullException.ThrowIfNull(source);
        var array = source as AppendedEvent[];
        if (array is not null && IsUnsubscribedArray(array, context))
        {
            return InapplicableArray(array);
        }

        var accumulator = new ObserverRunAccumulator(context, array is not null);
        try
        {
            if (array is not null)
            {
                ObserveArray(array, ref accumulator);
            }
            else
            {
                ObserveEnumerable(source, ref accumulator);
            }

            return accumulator.Complete();
        }
        finally
        {
            accumulator.Dispose();
        }
    }

    static bool IsUnsubscribedArray(AppendedEvent[] events, in ObserverRunPreparationContext context) =>
        !context.SubscribesToAllEvents && !HasSubscribedEvent(events, context.EventTypeIds);

    static PreparedObserverRun InapplicableArray(AppendedEvent[] events)
    {
        var last = events.Length == 0 ? Enumerable.Empty<AppendedEvent>().Last() : events[^1];
        return new(last.Context.SequenceNumber, false, []);
    }

    static bool HasSubscribedEvent(AppendedEvent[] events, IReadOnlySet<EventTypeId> eventTypeIds) =>
        eventTypeIds is FrozenSet<EventTypeId> frozen
            ? HasFrozenSubscription(events, frozen)
            : HasSetSubscription(events, eventTypeIds);

    static bool HasFrozenSubscription(AppendedEvent[] events, FrozenSet<EventTypeId> eventTypeIds)
    {
        foreach (var appendedEvent in events)
        {
            if (eventTypeIds.Contains(appendedEvent.Context.EventType.Id)) return true;
        }

        return false;
    }

    static bool HasSetSubscription(AppendedEvent[] events, IReadOnlySet<EventTypeId> eventTypeIds)
    {
        foreach (var appendedEvent in events)
        {
            if (eventTypeIds.Contains(appendedEvent.Context.EventType.Id)) return true;
        }

        return false;
    }

    static void ObserveArray(AppendedEvent[] events, ref ObserverRunAccumulator accumulator)
    {
        foreach (var appendedEvent in events)
        {
            accumulator.Observe(appendedEvent);
        }
    }

    static void ObserveEnumerable(IEnumerable<AppendedEvent> events, ref ObserverRunAccumulator accumulator)
    {
        foreach (var appendedEvent in events)
        {
            accumulator.Observe(appendedEvent);
        }
    }
}
