// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Merges handling progress with the snapshot returned by an awaited transition hook.
/// </summary>
internal static class ObserverTransitionStateMerge
{
    /// <summary>
    /// Keeps hook-owned changes without losing progress recorded while the hook awaited.
    /// </summary>
    /// <param name="handed">The snapshot handed to the hook.</param>
    /// <param name="recorded">The latest state recorded by handling while the hook awaited.</param>
    /// <param name="returned">The snapshot returned by the hook.</param>
    /// <returns>The merged state.</returns>
    internal static ObserverState Merge(ObserverState handed, ObserverState recorded, ObserverState returned)
    {
        var replayStarted = returned.IsReplaying && !handed.IsReplaying;
        var hookOwnsHandledCounts = HookOwnsHandledCounts(handed, returned);
        return returned with
        {
            NextEventSequenceNumber = MergePosition(handed.NextEventSequenceNumber, recorded.NextEventSequenceNumber, returned.NextEventSequenceNumber, replayStarted),
            LastHandledEventSequenceNumber = MergePosition(handed.LastHandledEventSequenceNumber, recorded.LastHandledEventSequenceNumber, returned.LastHandledEventSequenceNumber, replayStarted),
            TailEventSequenceNumber = MergePosition(handed.TailEventSequenceNumber, recorded.TailEventSequenceNumber, returned.TailEventSequenceNumber, replayStarted),
            InFlightPartitions = MergeInFlightPartitions(handed, recorded, returned),
            HandledEventCount = hookOwnsHandledCounts ? returned.HandledEventCount : recorded.HandledEventCount,
            HandledEventCountPerEventType = hookOwnsHandledCounts ? returned.HandledEventCountPerEventType : recorded.HandledEventCountPerEventType
        };
    }

    static EventSequenceNumber MergePosition(EventSequenceNumber handed, EventSequenceNumber recorded, EventSequenceNumber returned, bool replayStarted)
    {
        if (replayStarted || recorded == handed)
        {
            return returned;
        }

        if (returned == handed)
        {
            return recorded;
        }

        if (!returned.IsActualValue)
        {
            return recorded;
        }

        return !recorded.IsActualValue || returned > recorded ? returned : recorded;
    }

    static ISet<Key> MergeInFlightPartitions(ObserverState handed, ObserverState recorded, ObserverState returned)
    {
        var partitions = recorded.InFlightPartitions.ToHashSet();
        partitions.ExceptWith(handed.InFlightPartitions.Except(returned.InFlightPartitions));
        partitions.UnionWith(returned.InFlightPartitions.Except(handed.InFlightPartitions));
        return partitions;
    }

    // Replay owns even an unchanged zero/empty reset; otherwise count changes by value mark hook ownership.
    static bool HookOwnsHandledCounts(ObserverState handed, ObserverState returned) =>
        (returned.IsReplaying && !handed.IsReplaying) ||
        returned.HandledEventCount != handed.HandledEventCount ||
        !HasSameCounts(returned.HandledEventCountPerEventType, handed.HandledEventCountPerEventType);

    static bool HasSameCounts(IReadOnlyDictionary<EventTypeId, EventCount> left, IReadOnlyDictionary<EventTypeId, EventCount> right) =>
        ReferenceEquals(left, right) ||
        (left.Count == right.Count && left.All(_ => right.TryGetValue(_.Key, out var count) && count == _.Value));
}
