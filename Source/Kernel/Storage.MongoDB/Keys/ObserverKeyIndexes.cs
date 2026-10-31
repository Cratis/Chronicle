// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Keys;

namespace Cratis.Chronicle.Storage.MongoDB.Keys;

/// <summary>
/// Represents an implementation of <see cref="IObserverKeyIndex"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ObserverKeyIndexes"/> class.
/// </remarks>
/// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/>.</param>
public class ObserverKeyIndexes(IEventStoreNamespaceDatabase database) : IObserverKeyIndexes
{
    /// <inheritdoc/>
    public Task<IObserverKeyIndex> GetFor(ObserverKey observerKey, ObserverReaderScope scope)
    {
        var collection = database.GetEventSequenceCollectionFor(observerKey.EventSequenceId);
        return Task.FromResult<IObserverKeyIndex>(new EventSourceKeyIndex(collection, scope));
    }
}
