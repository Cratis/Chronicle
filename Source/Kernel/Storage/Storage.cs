// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage;

/// <summary>
/// Represents an implementation of <see cref="IStorage"/> for MongoDB.
/// </summary>
/// <param name="clusterStorage">The <see cref="IClusterStorage"/> instance.</param>
/// <param name="systemStorage">The <see cref="ISystemStorage"/> instance.</param>
/// <param name="sinkFactories"><see cref="ISinkFactory"/> instances.</param>
public class Storage(
    IClusterStorage clusterStorage,
    ISystemStorage systemStorage,
    IInstancesOf<ISinkFactory> sinkFactories) : IStorage
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = [];

    /// <inheritdoc/>
    public ISystemStorage System => systemStorage;

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreName>> GetEventStores() => clusterStorage.GetEventStores();

    /// <inheritdoc/>
    public async Task<bool> HasEventStore(EventStoreName eventStore)
    {
        var eventStores = await clusterStorage.GetEventStores();
        return eventStores.Any(es => es == eventStore);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() => clusterStorage.ObserveEventStores();

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
        // Use Task.Run to avoid deadlocking Orleans grain task schedulers: the async continuations inside these
        // methods must not be posted back to the Orleans ActivationTaskScheduler, which would be the case if called
        // directly with GetAwaiter().GetResult() from within a grain method.
        var eventStores = Task.Run(clusterStorage.GetEventStores).GetAwaiter().GetResult();
        var existingEventStore = eventStores.SingleOrDefault(e => e == eventStore);
        if (existingEventStore is null)
        {
            Task.Run(() => clusterStorage.SaveEventStore(eventStore)).GetAwaiter().GetResult();
        }

        return _eventStores[eventStore] =
                clusterStorage.CreateStorageForEventStore(
                    eventStore,
                    (eventStoreNamespaceName) =>
                        new Sinks.Sinks(eventStore, eventStoreNamespaceName, sinkFactories));
    }
}
