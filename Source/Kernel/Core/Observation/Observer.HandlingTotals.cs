// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Returns a new <see cref="ObserverState"/> with the running handled-event totals incremented by the
    /// counts of a single handled batch. The per-partition breakdown lives in the dedicated
    /// <see cref="IObserverHandledCountsStorage"/> and is not part of <see cref="ObserverState"/>.
    /// </summary>
    /// <param name="state">The current <see cref="ObserverState"/> to update.</param>
    /// <param name="countsPerEventType">The number of events handled in the batch, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>A new <see cref="ObserverState"/> with its handled-event totals incremented accordingly.</returns>
    static ObserverState WithIncrementedRunningTotals(
        ObserverState state,
        IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType)
    {
        if (countsPerEventType.Count == 0)
        {
            return state;
        }

        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        var total = EventCount.Zero;
        foreach (var (eventTypeId, count) in countsPerEventType)
        {
            perEventType[eventTypeId] = perEventType.GetValueOrDefault(eventTypeId, EventCount.Zero) + count.Value;
            total += count;
        }

        return state with
        {
            HandledEventCount = state.HandledEventCount + total,
            HandledEventCountPerEventType = perEventType
        };
    }

    /// <summary>
    /// Returns a new observer state with one partition's counts subtracted from the running totals, clamped at zero.
    /// </summary>
    /// <param name="state">The current state.</param>
    /// <param name="partitionCounts">The partition counts to subtract.</param>
    /// <returns>The updated state, or the original state when no counts are supplied.</returns>
    static ObserverState WithSubtractedPartitionHandledEventCounts(ObserverState state, Dictionary<EventTypeId, EventCount> partitionCounts)
    {
        if (partitionCounts.Count == 0) return state;
        var perEventType = new Dictionary<EventTypeId, EventCount>(state.HandledEventCountPerEventType);
        ulong partitionTotal = 0;
        foreach (var (eventTypeId, count) in partitionCounts)
        {
            partitionTotal += count.Value;
            if (!perEventType.TryGetValue(eventTypeId, out var current)) continue;
            if (current.Value <= count.Value) perEventType.Remove(eventTypeId);
            else perEventType[eventTypeId] = new EventCount(current.Value - count.Value);
        }
        var total = state.HandledEventCount.Value;
        return state with
        {
            HandledEventCount = new EventCount(total > partitionTotal ? total - partitionTotal : 0),
            HandledEventCountPerEventType = perEventType
        };
    }
}
