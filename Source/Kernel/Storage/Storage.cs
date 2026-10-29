// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Reactive;
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
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = new(CaseInsensitiveEventStoreNameComparer.Instance);

    /// <inheritdoc/>
    public ISystemStorage System => systemStorage;

    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<EventStoreName, Concepts.Projections.Generations.Enablement.GenerationFloor>> ReadGenerationFloors() =>
        clusterStorage.ReadGenerationFloors();

    /// <inheritdoc/>
    public async Task<IEnumerable<EventStoreName>> GetEventStores() => (await clusterStorage.GetEventStores()).Where(IsListed);

    /// <inheritdoc/>
    public async Task<bool> HasEventStore(EventStoreName eventStore)
    {
        var eventStores = await GetEventStores();
        return eventStores.Any(es => es == eventStore);
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventStoreName>> ObserveEventStores() =>
        new TransformingSubject<IEnumerable<EventStoreName>, IEnumerable<EventStoreName>>(
            clusterStorage.ObserveEventStores(),
            names => names.Where(IsListed));

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStoreName eventStore)
    {
        ThrowIfEventStoreNameIsInvalid(eventStore);

        if (_eventStores.TryGetValue(eventStore, out var existing))
        {
            return existing;
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

        var created = clusterStorage.CreateStorageForEventStore(
            eventStore,
            (eventStoreNamespaceName) =>
                new Sinks.Sinks(eventStore, eventStoreNamespaceName, sinkFactories));

        return _eventStores.GetOrAdd(eventStore, created);
    }

    /// <inheritdoc/>
    public void Clear() => _eventStores.Clear();

    static bool IsListed(EventStoreName name) => !string.IsNullOrWhiteSpace(name?.Value) && name != EventStoreName.NotSet;

    static void ThrowIfEventStoreNameIsInvalid(EventStoreName eventStore)
    {
        if (eventStore is null || string.IsNullOrWhiteSpace(eventStore.Value))
        {
            throw new InvalidEventStoreName(eventStore!, "EventStoreName cannot be null, empty or whitespace.");
        }

        if (eventStore == EventStoreName.NotSet)
        {
            throw new InvalidEventStoreName(eventStore, "EventStoreName cannot be '[NotSet]'. It must be properly configured.");
        }
    }
}
