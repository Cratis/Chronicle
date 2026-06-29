// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.InMemory;

/// <summary>
/// Represents the process-wide registry of in-memory <see cref="IEventStoreStorage"/> instances.
/// </summary>
/// <remarks>
/// Both <see cref="Storage"/> (node-level <see cref="IStorage"/>) and <see cref="ClusterStorage"/>
/// (<see cref="IClusterStorage"/>) resolve event-store storage through this single registry, so the
/// same in-memory event log is observed regardless of which entry point is used.
/// </remarks>
/// <param name="sinkFactories">All discovered <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> for resolving job state types.</param>
public sealed class EventStoreStorages(IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes) : IDisposable
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = new();
    readonly ReplaySubject<IEnumerable<EventStoreName>> _namesSubject = new(1);

    /// <summary>
    /// Gets all the <see cref="EventStoreName">event stores</see> currently registered.
    /// </summary>
    public IEnumerable<EventStoreName> Names => [.. _eventStores.Keys];

    /// <summary>
    /// Gets an observable stream of the registered <see cref="EventStoreName">event stores</see>.
    /// </summary>
    public ISubject<IEnumerable<EventStoreName>> Observe => _namesSubject;

    /// <summary>
    /// Determines whether the registry contains a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to check for.</param>
    /// <returns>True if present, false otherwise.</returns>
    public bool Has(EventStoreName eventStore) => _eventStores.ContainsKey(eventStore);

    /// <summary>
    /// Gets or creates the <see cref="IEventStoreStorage"/> for a specific <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get storage for.</param>
    /// <param name="sinksFactory">Optional <see cref="SinksFactory"/> to use; when omitted a default factory is created from the discovered sink factories.</param>
    /// <returns>The <see cref="IEventStoreStorage"/> for the event store.</returns>
    public IEventStoreStorage GetOrCreate(EventStoreName eventStore, SinksFactory? sinksFactory = null)
    {
        if (_eventStores.TryGetValue(eventStore, out var existing))
        {
            return existing;
        }

        var created = new EventStoreStorage(eventStore, sinksFactory ?? CreateDefaultSinksFactory(eventStore), jobTypes);
        var storage = _eventStores.GetOrAdd(eventStore, created);

        if (ReferenceEquals(storage, created))
        {
            _namesSubject.OnNext([.. _eventStores.Keys]);
        }

        return storage;
    }

    /// <summary>
    /// Clears all registered event stores.
    /// </summary>
    public void Clear()
    {
        _eventStores.Clear();
        _namesSubject.OnNext([]);
    }

    /// <inheritdoc/>
    public void Dispose() => _namesSubject.Dispose();

    SinksFactory CreateDefaultSinksFactory(EventStoreName eventStore) =>
        @namespace => new Cratis.Chronicle.Storage.Sinks.Sinks(eventStore, @namespace, sinkFactories);
}
