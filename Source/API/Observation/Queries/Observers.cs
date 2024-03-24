// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Observation;
using Cratis.Kernel.Storage;
using Cratis.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.API.Observation.Queries;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{eventStore}/{namespace}/observers")]
public class Observers(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the observers are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace) =>
         storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers.GetAllObservers();

    /// <summary>
    /// Get and observe all observers for an event store and namespace.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> the observers are for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<ObserverInformation>>> AllObservers(
        [FromRoute] EventStoreName eventStore,
        [FromRoute] EventStoreNamespaceName @namespace)
    {
        var clientObservable = new ClientObservable<IEnumerable<ObserverInformation>>();
        var observers = storage.GetEventStore(eventStore).GetNamespace(@namespace).Observers;
        var observable = observers.ObserveAll();
        var subscription = observable.Subscribe(clientObservable.OnNext);
        clientObservable.ClientDisconnected = () =>
        {
            subscription.Dispose();
            if (observable is IDisposable disposableObservable)
            {
                disposableObservable.Dispose();
            }
        };

        return Task.FromResult(clientObservable);
    }
}
