// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <param name="clusterStorage">The <see cref="IClusterStorage"/> instance.</param>
public class Storage(IClusterStorage clusterStorage) : IStorage
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = [];

    /// <inheritdoc/>
    public IClusterStorage Cluster => clusterStorage;

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        var pair = _eventStores
            .Select(kvp => new { kvp.Key, kvp.Value })
            .FirstOrDefault(_ => _.Key.Value.Equals(eventStore.Value, StringComparison.InvariantCultureIgnoreCase));

        if (pair is not null)
        {
            return pair.Value;
        }

        return _eventStores[eventStore] = clusterStorage.CreateStorageForEventStore(eventStore);
    }
}
