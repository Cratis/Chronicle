// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Applications.Queries;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.DependencyInversion;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.Cratis.Kernel.Read.Observation;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/observers")]
public class Observers : Controller
{
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Observers(
        ProviderFor<IObserverStorage> observerStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _observerStorageProvider = observerStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Collection of <see cref="ObserverInformation"/>.</returns>
    [HttpGet]
    public Task<IEnumerable<ObserverInformation>> GetObservers(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);

        return _observerStorageProvider().GetAllObservers();
    }

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
        _executionContextManager.Establish(tenantId, _executionContextManager.Current.CorrelationId, microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<ObserverInformation>>();
        var observable = _observerStorageProvider().All;
        var subscription = observable.Subscribe(_ => clientObservable.OnNext(_));
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
