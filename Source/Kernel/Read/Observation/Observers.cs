// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Cratis.Kernel.Observation;
using Cratis.Kernel.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Kernel.Read.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Observers"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing underlying storage.</param>
[Route("/api/events/store/{microserviceId}/{tenantId}/observers")]
public class Observers(IStorage storage) : ControllerBase
{
    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId) =>
         storage.GetEventStore((string)microserviceId).GetNamespace(tenantId).Observers.GetAllObservers();

    /// <summary>
    /// Get and observe all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet("observe")]
    public Task<ClientObservable<IEnumerable<ObserverInformation>>> AllObservers(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        var clientObservable = new ClientObservable<IEnumerable<ObserverInformation>>();
        var observers = storage.GetEventStore((string)microserviceId).GetNamespace(tenantId).Observers;
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
