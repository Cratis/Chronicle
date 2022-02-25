// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="failedObservers"><see cref="IFailedObservers"/> for getting all failed observers.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for activating failed observers.</param>
    public Observers(
        IFailedObservers failedObservers,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory)
    {
        _failedObservers = failedObservers;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public override async Task OnActivateAsync()
    {
        _microserviceId = _executionContextManager.Current.MicroserviceId;
        _tenantId = _executionContextManager.Current.TenantId;
        await base.OnActivateAsync();
    }

    /// <inheritdoc/>
    public async Task RetryFailed()
    {
        var observers = await _failedObservers.GetAll();
        foreach (var observer in observers)
        {
            var key = new PartitionedObserverKey(_microserviceId, _tenantId, observer.EventSequenceId, observer.EventSourceId);
            var partitionedObserver = _grainFactory.GetGrain<IPartitionedObserver>(observer.ObserverId, keyExtension: key.ToString());
            await partitionedObserver.TryResume();
        }
    }
}
