// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines storage for progress parked independently for each observer and event sequence.
/// </summary>
public interface IObserverSequenceCheckpointsStorage
{
    /// <summary>
    /// Gets a parked checkpoint without manufacturing progress for an unseen sequence.
    /// </summary>
    /// <param name="observerId">The observer.</param>
    /// <param name="eventSequenceId">The sequence.</param>
    /// <returns>The parked checkpoint, or null when none exists.</returns>
    Task<ObserverSequenceCheckpoint?> Get(ObserverId observerId, EventSequenceId eventSequenceId);

    /// <summary>
    /// Inserts or replaces the checkpoint identified by its observer and sequence.
    /// </summary>
    /// <param name="checkpoint">The checkpoint to store.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task Save(ObserverSequenceCheckpoint checkpoint);

    /// <summary>
    /// Removes every checkpoint belonging to an observer.
    /// </summary>
    /// <param name="observerId">The observer.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RemoveAllFor(ObserverId observerId);
}
