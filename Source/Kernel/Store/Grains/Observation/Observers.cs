// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store.Grains.Connections;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.Orleans.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : Grain, IObservers
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IRequestContextManager _requestContextManager;
    readonly IGrainFactory _grainFactory;
    readonly Tenants _tenants;
    IConnectedClients? _connectedClients;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="requestContextManager"><see cref="IRequestContextManager"/> for working with the Orleans request context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for activating other grains.</param>
    /// <param name="tenants">All configured <see cref="Tenants"/>.</param>
    public Observers(
        IExecutionContextManager executionContextManager,
        IRequestContextManager requestContextManager,
        IGrainFactory grainFactory,
        Tenants tenants)
    {
        _executionContextManager = executionContextManager;
        _requestContextManager = requestContextManager;
        _grainFactory = grainFactory;
        _tenants = tenants;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _microserviceId = _executionContextManager.Current.MicroserviceId;
        _connectedClients = _grainFactory.GetGrain<IConnectedClients>(Guid.Empty);
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Subscribe(ObserverId observerId, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes)
    {
        var connectionId = _requestContextManager.Get(RequestContextKeys.ConnectionId).ToString()!;
        foreach (var tenantId in _tenants.GetTenantIds())
        {
            var observerKey = new ObserverKey(_microserviceId, tenantId, eventSequenceId);
            var observer = _grainFactory.GetGrain<IObserver>(observerId, observerKey);
            await observer.SetConnectionId(connectionId);
            await observer.Subscribe(eventTypes);
            await _connectedClients!.SubscribeOnDisconnected(connectionId, observer);
        }
    }

    /// <inheritdoc/>
    public async Task RetryFailed()
    {
        // foreach (var tenantId in _tenants.GetTenantIds())
        // {
        //     var observers = await _failedObservers.GetAll();
        //     foreach (var observer in observers)
        //     {
        //         var key = new PartitionedObserverKey(_microserviceId, tenantId, observer.EventSequenceId, observer.EventSourceId);
        //         var partitionedObserver = _grainFactory.GetGrain<IPartitionedObserver>(observer.ObserverId, keyExtension: key);
        //         await partitionedObserver.TryResume();
        //     }
        // }
        await Task.CompletedTask;
    }
}
