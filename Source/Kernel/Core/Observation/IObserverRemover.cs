// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that removes an observer and everything keyed to it from an event store.
/// </summary>
public interface IObserverRemover
{
    /// <summary>
    /// Remove an observer and everything keyed to it.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the observer belongs to.</param>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer to remove.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the observer observes.</param>
    /// <returns>An <see cref="ObserverRemovalResult"/> describing what happened.</returns>
    /// <remarks>
    /// Refuses while the observer is running or has a subscribed client in any namespace, so the operation only
    /// ever reaches an observer no client is reporting.
    /// </remarks>
    Task<ObserverRemovalResult> Remove(EventStoreName eventStore, ObserverId observerId, EventSequenceId eventSequenceId);
}
