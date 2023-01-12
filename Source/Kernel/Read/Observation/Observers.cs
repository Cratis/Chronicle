// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Applications.Queries;
using Aksio.Cratis.Events.Store.Grains.Observation;
using Aksio.Cratis.Execution;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Aksio.Cratis.Events.Store.Observation.Api;

/// <summary>
/// Represents the API for working with observers.
/// </summary>
[Route("/api/events/store/{microserviceId}/{tenantId}/observers")]
public class Observers : Controller
{
    readonly IObserversState _observersState;
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="observersState"><see cref="IObserversState"/> for working with the state of observers.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public Observers(
        IObserversState observersState,
        IGrainFactory grainFactory,
        IExecutionContextManager executionContextManager)
    {
        _observersState = observersState;
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Get and observe all observers.
    /// </summary>
    /// <param name="microserviceId"><see cref="MicroserviceId"/> the observers are for.</param>
    /// <param name="tenantId"><see cref="TenantId"/> the observers are for.</param>
    /// <returns>Client observable of a collection of <see cref="ObserverState"/>.</returns>
    [HttpGet]
    public Task<ClientObservable<IEnumerable<ObserverState>>> AllObservers(
        [FromRoute] MicroserviceId microserviceId,
        [FromRoute] TenantId tenantId)
    {
        _executionContextManager.Establish(tenantId, CorrelationId.New(), microserviceId);

        var clientObservable = new ClientObservable<IEnumerable<ObserverState>>();
        var observable = _observersState.All;
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
