// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Reactions;
using Cratis.Chronicle.Storage.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverKeyIndexes"/> class.
/// </remarks>
/// <param name="eventStoreDatabase">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
/// <param name="observerStorage">Provider for <see cref="IObserverStorage"/>.</param>
public class ObserverKeyIndexes(
    IEventStoreNamespaceDatabase eventStoreDatabase,
    IObserverStorage observerStorage) : IObserverKeyIndexes
{
    /// <inheritdoc/>
    public async Task<IObserverKeyIndex> GetFor(
        ObserverId observerId,
        ObserverKey observerKey)
    {
        var observer = await observerStorage.Get(observerId);
        var database = eventStoreDatabase;
        var collection = database.GetEventSequenceCollectionFor(observerKey.EventSequenceId);
        return new EventSourceKeyIndex(collection, observer.EventTypes);
    }
}
