// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Storage.Keys;
using Aksio.Cratis.Kernel.Storage.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class ObserverKeyIndexes : IObserverKeyIndexes
{
    readonly ProviderFor<IEventStoreInstanceDatabase> _eventStoreDatabaseProvider;
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverKeyIndexes"/> class.
    /// </summary>
    /// <param name="eventStoreDatabaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/>.</param>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    public ObserverKeyIndexes(
        ProviderFor<IEventStoreInstanceDatabase> eventStoreDatabaseProvider,
        ProviderFor<IObserverStorage> observerStorageProvider,
        IExecutionContextManager executionContextManager)
    {
        _eventStoreDatabaseProvider = eventStoreDatabaseProvider;
        _observerStorageProvider = observerStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    public async Task<IObserverKeyIndex> GetFor(
        ObserverId observerId,
        ObserverKey observerKey)
    {
        _executionContextManager.Establish(observerKey.TenantId, CorrelationId.New(), observerKey.MicroserviceId);
        var observer = await _observerStorageProvider().GetObserver(observerId);
        var database = _eventStoreDatabaseProvider();
        var collection = database.GetEventSequenceCollectionFor(observerKey.EventSequenceId);
        return new EventSourceKeyIndex(collection, observer.EventTypes);
    }
}
