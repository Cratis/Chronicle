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
    public async Task RetryFailed()
    {
        // TODO: do this for for all tenants
        const string tenant = "3352d47d-c154-4457-b3fb-8a2efb725113";
        _executionContextManager.Establish(tenant, CorrelationId.New());

        var observers = await _failedObservers.GetAll();
        foreach (var observer in observers)
        {
            var key = PartitionedObserverKeyHelper.Create(tenant, observer.EventLogId, observer.EventSourceId);
            var partitionedObserver = _grainFactory.GetGrain<IPartitionedObserver>(observer.ObserverId, keyExtension: key);
            await partitionedObserver.TryResume();
        }
    }
}
