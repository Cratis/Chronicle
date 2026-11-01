// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Moves an observer's stored progress and failed partitions from one event sequence to another in a namespace.
/// </summary>
/// <remarks>
/// The progress of the sequence being left is parked, and the target sequence's own parked progress is selected, or a
/// fresh start when the observer never observed it. Failures stay where they are and are tagged with their sequence.
/// Every step can be repeated: the compare-and-set of the stored owner commits the move together with the cursor, so
/// a handover interrupted before it simply runs again.
/// </remarks>
internal static class ObserverSequenceHandover
{
    const int MaxAttempts = 3;

    /// <summary>
    /// Hands the stored progress of an observer over to a sequence.
    /// </summary>
    /// <param name="namespaceStorage">The storage of the namespace to hand over in.</param>
    /// <param name="observerId">The observer.</param>
    /// <param name="from">The sequence the observer is assumed to be on when its stored state names no owner.</param>
    /// <param name="to">The sequence to hand over to.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The <see cref="SequenceHandoverResult"/>.</returns>
    public static async Task<SequenceHandoverResult> HandOver(
        IEventStoreNamespaceStorage namespaceStorage,
        ObserverId observerId,
        EventSequenceId from,
        EventSequenceId to,
        ILogger<Observer> logger)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var current = await namespaceStorage.Observers.Get(observerId);
            if (current.Identifier != observerId || (current.Owner is null && from == EventSequenceId.Unspecified))
            {
                await namespaceStorage.Observers.ClaimOwnership(observerId, to);
                return SequenceHandoverResult.Claimed;
            }

            var owner = current.Owner ?? from;
            if (owner == to)
            {
                return SequenceHandoverResult.AlreadyOnTarget;
            }

            var failures = await StampFailures(namespaceStorage, observerId, owner);
            await namespaceStorage.ObserverSequenceCheckpoints.Save(ObserverSequenceCheckpoint.ParkedFrom(current, owner));

            var target = await namespaceStorage.ObserverSequenceCheckpoints.Get(observerId, to) ?? ObserverSequenceCheckpoint.Fresh(observerId, to);
            target = target with { FailedPartitionCount = failures.Count(failure => failure.EventSequenceId == to) };
            if (await namespaceStorage.Observers.HandOver(observerId, current.Owner, target))
            {
                logger.HandedOverToEventSequence(observerId, owner, to);
                return SequenceHandoverResult.HandedOver;
            }

            logger.HandOverOwnerChanged(observerId, to, attempt);
        }

        logger.HandOverGaveUp(observerId, to);
        return SequenceHandoverResult.NotHandedOver;
    }

    static async Task<FailedPartition[]> StampFailures(IEventStoreNamespaceStorage namespaceStorage, ObserverId observerId, EventSequenceId owner)
    {
        var failures = (await namespaceStorage.FailedPartitions.GetFor(observerId)).Partitions.ToArray();
        var untagged = failures.Where(failure => failure.EventSequenceId is null).ToArray();
        if (untagged.Length == 0)
        {
            return failures;
        }

        foreach (var failure in untagged)
        {
            failure.EventSequenceId = owner;
        }

        await namespaceStorage.FailedPartitions.Save(observerId, new() { Partitions = untagged });
        return failures;
    }
}
