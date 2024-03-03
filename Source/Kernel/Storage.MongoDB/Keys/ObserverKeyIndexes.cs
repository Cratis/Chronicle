// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Storage.Keys;
using Cratis.Kernel.Storage.Observation;
using Cratis.Observation;

namespace Cratis.Kernel.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
public class ObserverKeyIndexes : IObserverKeyIndexes
{
    readonly IEventStoreNamespaceDatabase _eventStoreDatabase;
    readonly IObserverStorage _observerStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverKeyIndexes"/> class.
    /// </summary>
    /// <param name="eventStoreDatabase">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
    /// <param name="observerStorage">Provider for <see cref="IObserverStorage"/>.</param>
    public ObserverKeyIndexes(
        IEventStoreNamespaceDatabase eventStoreDatabase,
        IObserverStorage observerStorage)
    {
        _eventStoreDatabase = eventStoreDatabase;
        _observerStorage = observerStorage;
    }

    /// <inheritdoc/>
    public async Task<IObserverKeyIndex> GetFor(
        ObserverId observerId,
        ObserverKey observerKey)
    {
        var observer = await _observerStorage.GetObserver(observerId);
        var database = _eventStoreDatabase;
        var collection = database.GetEventSequenceCollectionFor(observerKey.EventSequenceId);
        return new EventSourceKeyIndex(collection, observer.EventTypes);
    }
}
