// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines a storage system for the number of events an observer has successfully handled per partition,
/// broken down by event type identifier.
/// </summary>
/// <remarks>
/// These counts are kept in a dedicated store — keyed by <c>(observerId, partition)</c> and updated with
/// atomic increments — rather than embedded in <see cref="ObserverState"/>, whose per-partition breakdown
/// grew unbounded with every event source ever seen and was rewritten wholesale on every handled batch. The
/// counts exist so that a single partition's contribution can be subtracted from the observer's running
/// totals when that partition is replayed. They are derived statistics: a crash between an increment here and
/// the subsequent <see cref="ObserverState"/> write may cause a minor drift that a full replay resets. They
/// are never part of the exactly-once event-delivery bookkeeping, which is driven by the sequence numbers on
/// <see cref="ObserverState"/> and the in-flight events store.
/// </remarks>
public interface IObserverHandledCountsStorage
{
    /// <summary>
    /// Increment the handled-event counts for a partition by the counts of a single handled batch.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> the counts are for.</param>
    /// <param name="partition">The partition <see cref="Key"/> the counts are for.</param>
    /// <param name="countsPerEventType">The number of events handled in the batch, broken down by <see cref="EventTypeId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Increment(ObserverId observerId, Key partition, IReadOnlyDictionary<EventTypeId, EventCount> countsPerEventType);

    /// <summary>
    /// Get the handled-event counts recorded for a partition, broken down by event type identifier.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get for.</param>
    /// <param name="partition">The partition <see cref="Key"/> to get for.</param>
    /// <returns>The counts per <see cref="EventTypeId"/>; empty if none are recorded.</returns>
    Task<IReadOnlyDictionary<EventTypeId, EventCount>> GetFor(ObserverId observerId, Key partition);

    /// <summary>
    /// Remove the recorded handled-event counts for a single partition. Used when a partition replay begins.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> the counts are for.</param>
    /// <param name="partition">The partition <see cref="Key"/> to remove.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveFor(ObserverId observerId, Key partition);

    /// <summary>
    /// Remove all recorded handled-event counts for an observer. Used when a full replay begins.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to remove all counts for.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveAllFor(ObserverId observerId);
}
