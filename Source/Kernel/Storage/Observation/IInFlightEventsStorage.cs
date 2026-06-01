// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines a storage system for tracking <see cref="InFlightEvent"/> entries.
/// </summary>
/// <remarks>
/// In-flight events are persisted independently of <see cref="ObserverState"/> so that the observer can
/// recover deterministically after a crash that interrupted multi-partition handling. Entries are added
/// before a partition handler is invoked and removed once that handler reports successful completion.
/// </remarks>
public interface IInFlightEventsStorage
{
    /// <summary>
    /// Record an <see cref="InFlightEvent"/> entry for an event that is about to be handled.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> the event is for.</param>
    /// <param name="partition">The partition <see cref="Key"/> the event belongs to.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event in flight.</param>
    /// <returns>Awaitable task.</returns>
    Task Add(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber);

    /// <summary>
    /// Remove an <see cref="InFlightEvent"/> entry for an event that completed successfully.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> the event is for.</param>
    /// <param name="partition">The partition <see cref="Key"/> the event belongs to.</param>
    /// <param name="eventSequenceNumber">The <see cref="EventSequenceNumber"/> of the event to clear.</param>
    /// <returns>Awaitable task.</returns>
    Task Remove(ObserverId observerId, Key partition, EventSequenceNumber eventSequenceNumber);

    /// <summary>
    /// Remove every <see cref="InFlightEvent"/> entry for a partition up to and including a given
    /// <see cref="EventSequenceNumber"/>.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> the events are for.</param>
    /// <param name="partition">The partition <see cref="Key"/> to clear.</param>
    /// <param name="upToInclusive">The <see cref="EventSequenceNumber"/> up to (and including) which entries should be removed.</param>
    /// <returns>Awaitable task.</returns>
    Task RemoveUpTo(ObserverId observerId, Key partition, EventSequenceNumber upToInclusive);

    /// <summary>
    /// Get all currently recorded <see cref="InFlightEvent"/> entries for an observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get entries for.</param>
    /// <returns>Collection of <see cref="InFlightEvent"/> entries (empty if none).</returns>
    Task<IEnumerable<InFlightEvent>> GetFor(ObserverId observerId);
}
