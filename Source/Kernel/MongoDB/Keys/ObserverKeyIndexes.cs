// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Persistence.Keys;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class ObserverKeyIndexes : IObserverKeyIndexes
{
    readonly ProviderFor<IEventStoreInstanceDatabase> _eventStoreDatabaseProvider;
    readonly IExecutionContextManager _executionContextManager;
    readonly IGrainFactory _grainFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverKeyIndexes"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    public ObserverKeyIndexes(
        ProviderFor<IEventStoreInstanceDatabase> eventStoreDatabaseProvider,
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public async Task<IObserverKeyIndex> GetFor(
        ObserverId observerId,
        ObserverKey observerKey)
    {
        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);
        var observer = _grainFactory.GetGrain<IObserver>(observerId, keyExtension: observerKey);
        var eventTypes = await observer.GetEventTypes();

        var database = _eventStoreDatabaseProvider();
        var collection = database.GetEventSequenceCollectionFor(observerKey.EventSequenceId);
        return new EventSourceKeyIndex(collection, eventTypes);
    }
}
