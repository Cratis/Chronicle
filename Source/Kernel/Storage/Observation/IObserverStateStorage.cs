// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Defines a storage provider for working with observers.
/// </summary>
public interface IObserverStateStorage
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="ObserverState"/>.
    /// </summary>
    /// <returns>An observable of collection of <see cref="ObserverState"/>.</returns>
    ISubject<IEnumerable<ObserverState>> ObserveAll();

    /// <summary>
    /// Get the information for a specific observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get for.</param>
    /// <returns>The <see cref="ObserverState"/>, or <see cref="ObserverState.Empty"/> when no record exists.</returns>
    Task<ObserverState> Get(ObserverId observerId);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverState"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverState>> GetAll();

    /// <summary>
    /// Gets the identifiers of retired observers among the submitted candidates without loading their state.
    /// </summary>
    /// <param name="observerIds">The candidate observer identifiers.</param>
    /// <returns>The identifiers of candidates whose alert disposition is retired.</returns>
    Task<IEnumerable<ObserverId>> GetRetired(IEnumerable<ObserverId> observerIds);

    /// <summary>
    /// Save the state of an observer.
    /// </summary>
    /// <param name="state"><see cref="ObserverState"/> to save.</param>
    /// <param name="owner">The event sequence addressed by the writing activation.</param>
    /// <returns>Whether the state was saved. False means another sequence owns the record.</returns>
    /// <remarks>
    /// A missing owner on legacy state counts as unowned; the next successful save stamps it.
    /// During a rolling upgrade older kernels preserve but ignore Owner, so their writes bypass the owner guard until the next newer write.
    /// </remarks>
    Task<bool> Save(ObserverState state, EventSequenceId owner);

    /// <summary>
    /// Stamps ownership on an existing state record without creating one.
    /// </summary>
    /// <param name="observerId">The observer whose ownership changed.</param>
    /// <param name="owner">The event sequence that now owns the observer.</param>
    /// <returns>Awaitable task.</returns>
    Task ClaimOwnership(ObserverId observerId, EventSequenceId owner);

    /// <summary>
    /// Gets when the running state of an observer last changed.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get for.</param>
    /// <returns>The time of the last settled running-state change, or null when unknown or no record exists.</returns>
    /// <remarks>
    /// Kept outside <see cref="ObserverState"/> so the per-batch state copies do not carry it.
    /// </remarks>
    Task<DateTimeOffset?> GetRunningStateChangedAt(ObserverId observerId);

    /// <summary>
    /// Gets when the running state last changed for all observers with a known value.
    /// </summary>
    /// <returns>The known times keyed by observer; observers without a known value are absent.</returns>
    Task<IReadOnlyDictionary<ObserverId, DateTimeOffset>> GetRunningStatesChangedAt();

    /// <summary>
    /// Saves when the running state of an existing observer last changed.
    /// </summary>
    /// <param name="observerId">The observer whose running state changed.</param>
    /// <param name="runningStateChangedAt">The time of the change, or null to clear it.</param>
    /// <param name="owner">The event sequence addressed by the writing activation.</param>
    /// <returns>Whether an existing record was updated. False means no record exists or another sequence owns it.</returns>
    /// <remarks>
    /// Never creates a record and never changes any other field. The owner guard is the same as for <see cref="Save"/>:
    /// a record owned by another sequence is refused, an unowned legacy record is accepted.
    /// </remarks>
    Task<bool> SaveRunningStateChangedAt(ObserverId observerId, DateTimeOffset? runningStateChangedAt, EventSequenceId owner);

    /// <summary>
    /// Delete the state of an observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to delete the state for.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Deleting the state of an observer that has none is not an error - removal has to be safe to repeat after a
    /// partial failure, and the caller's intent is satisfied either way.
    /// </remarks>
    Task Delete(ObserverId observerId);

    /// <summary>
    /// Rename an observer by its current identifier.
    /// </summary>
    /// <param name="currentId">The current <see cref="ObserverId"/>.</param>
    /// <param name="newId">The new <see cref="ObserverId"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Rename(ObserverId currentId, ObserverId newId);
}
