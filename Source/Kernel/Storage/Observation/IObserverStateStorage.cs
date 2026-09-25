// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
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
    /// <returns><see cref="ObserverState"/>.</returns>
    Task<ObserverState> Get(ObserverId observerId);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverState"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverState>> GetAll();

    /// <summary>
    /// Save the state of an observer.
    /// </summary>
    /// <param name="state"><see cref="ObserverState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task Save(ObserverState state);

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
