// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <param name="clusterStorage">The <see cref="IClusterStorage"/> instance.</param>
/// <param name="sinkFactories"><see cref="ISinkFactory"/> instances.</param>
public class Storage(
    IClusterStorage clusterStorage,
    IInstancesOf<ISinkFactory> sinkFactories) : IStorage
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

        // TODO: This logic should be replaced by formalizing event stores as a Grain and it ensuring existence. Service layer should do this.
        var eventStores = clusterStorage.GetEventStores().GetAwaiter().GetResult();
        var existingEventStore = eventStores.SingleOrDefault(e => e == eventStore);
        if (existingEventStore is null)
        {
            clusterStorage.SaveEventStore(eventStore).GetAwaiter().GetResult();
        }

        return _eventStores[eventStore] =
                clusterStorage.CreateStorageForEventStore(
                    eventStore,
                    (eventStoreNamespaceName) =>
                        new Sinks.Sinks(eventStore, eventStoreNamespaceName, sinkFactories));
    }
}
