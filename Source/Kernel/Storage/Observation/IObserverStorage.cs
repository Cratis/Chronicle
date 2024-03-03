// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Observation;

namespace Aksio.Cratis.Kernel.Storage.Observation;

/// <summary>
/// Defines a storage provider for working with observers.
/// </summary>
public interface IObserverStorage
{
    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> for all instances of <see cref="ObserverInformation"/>.
    /// </summary>
    /// <returns>An observable of collection of <see cref="ObserverInformation"/>.</returns>
    IObservable<IEnumerable<ObserverInformation>> ObserveAll();

    /// <summary>
    /// Get the information for a specific observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> to get for.</param>
    /// <returns><see cref="ObserverInformation"/>.</returns>
    Task<ObserverInformation> GetObserver(ObserverId observerId);

    /// <summary>
    /// Get all observers for specific event types.
    /// </summary>
    /// <param name="eventTypes">Collection of <see cref="EventType"/> to get for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetObserversForEventTypes(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <returns>Collection of <see cref="ObserverInformation"/> holding all information about the observers.</returns>
    Task<IEnumerable<ObserverInformation>> GetAllObservers();

    /// <summary>
    /// Get the state of an observer.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to get for.</param>
    /// <param name="observerKey"><see cref="ObserverKey"/> to get for.</param>
    /// <returns><see cref="ObserverState"/> for the observer.</returns>
    Task<ObserverState> GetState(ObserverId observerId, ObserverKey observerKey);

    /// <summary>
    /// Save the state of an observer.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to save for.</param>
    /// <param name="observerKey"><see cref="ObserverKey"/> to save for.</param>
    /// <param name="state"><see cref="ObserverState"/> to save.</param>
    /// <returns>Awaitable task.</returns>
    Task SaveState(ObserverId observerId, ObserverKey observerKey, ObserverState state);
}
