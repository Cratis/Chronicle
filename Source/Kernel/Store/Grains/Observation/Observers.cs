// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store.Observation;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : Grain, IObservers
{
    readonly IFailedObservers _failedObservers;
    readonly IExecutionContextManager _executionContextManager;
    readonly IGrainFactory _grainFactory;
    readonly Tenants _tenants;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="failedObservers"><see cref="IFailedObservers"/> for getting all failed observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for activating failed observers.</param>
    /// <param name="tenants">All configured <see cref="Tenants"/>.</param>
    public Observers(
        IFailedObservers failedObservers,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory,
        Tenants tenants)
    {
        _failedObservers = failedObservers;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
        _tenants = tenants;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _microserviceId = _executionContextManager.Current.MicroserviceId;
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task Subscribe(ObserverId observerId, EventSequenceId eventSequenceId, IEnumerable<EventType> eventTypes)
    {
        foreach (var tenantId in _tenants.GetTenantIds())
        {
            var observerKey = new ObserverKey(_microserviceId, tenantId, eventSequenceId);
            var observer = _grainFactory.GetGrain<IObserver>(observerId, observerKey);
            await observer.Subscribe(eventTypes);
        }
    }

    /// <inheritdoc/>
    public async Task RetryFailed()
    {
        foreach (var tenantId in _tenants.GetTenantIds())
        {
            var observers = await _failedObservers.GetAll();
            foreach (var observer in observers)
            {
                var key = new PartitionedObserverKey(_microserviceId, tenantId, observer.EventSequenceId, observer.EventSourceId);
                var partitionedObserver = _grainFactory.GetGrain<IPartitionedObserver>(observer.ObserverId, keyExtension: key);
                await partitionedObserver.TryResume();
            }
        }
    }
}
