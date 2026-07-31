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
/// <para>
/// Both <see cref="Storage"/> (node-level <see cref="IStorage"/>) and <see cref="ClusterStorage"/>
/// (<see cref="IClusterStorage"/>) resolve event-store storage through this single registry, so the
/// same in-memory event log is observed regardless of which entry point is used.
/// </para>
/// <para>
/// The registry fans changes out over an internal subject that is never handed to a caller. Every caller of
/// <see cref="Observe"/> gets its own subject, because callers complete the subject they are given when their
/// connection goes away (see <see cref="Reactive.ObservableExtensions.CompletedBy{TResult}"/>) and completing a
/// shared subject would silently end event-store observation for every other caller in the process.
/// </para>
/// </remarks>
/// <param name="sinkFactories">All discovered <see cref="ISinkFactory"/> instances.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> for resolving job state types.</param>
public sealed class EventStoreStorages(IInstancesOf<ISinkFactory> sinkFactories, IJobTypes jobTypes) : IDisposable
{
    readonly ConcurrentDictionary<EventStoreName, IEventStoreStorage> _eventStores = new();

    readonly Subject<IEnumerable<EventStoreName>> _changes = new();

    readonly Lock _publishing = new();

    /// <summary>
    /// Gets all the <see cref="EventStoreName">event stores</see> currently registered.
    /// </summary>
    public IEnumerable<EventStoreName> Names => [.. _eventStores.Keys];

    /// <summary>
    /// Creates an observable stream of the registered <see cref="EventStoreName">event stores</see>, seeded with the
    /// current set and updated as event stores are added.
    /// </summary>
    /// <returns>A subject dedicated to the caller, which the caller owns and may complete.</returns>
    /// <remarks>
    /// Each call returns its own subject - the same semantics the MongoDB implementation provides by handing out a
    /// fresh <see cref="BehaviorSubject{T}"/> per call. Completing it unsubscribes only that caller.
    /// </remarks>
    public ISubject<IEnumerable<EventStoreName>> Observe()
    {
        var subject = new ReplaySubject<IEnumerable<EventStoreName>>(1);
        IDisposable subscription;

        // Subscribing and seeding have to happen against a consistent set, or an event store added in between is
        // missed entirely - which is exactly when stores appear, since every connecting client ensures its own.
        lock (_publishing)
        {
            subscription = _changes.Subscribe(subject.OnNext);
            subject.OnNext(Names);
        }

        subject.Subscribe(_ => { }, _ => { }, subscription.Dispose);

        return subject;
    }

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
            Publish();
        }

        return storage;
    }

    /// <summary>
    /// Clears all registered event stores.
    /// </summary>
    public void Clear()
    {
        _eventStores.Clear();
        Publish();
    }

    /// <inheritdoc/>
    public void Dispose() => _changes.Dispose();

    void Publish()
    {
        lock (_publishing)
        {
            _changes.OnNext([.. _eventStores.Keys]);
        }
    }

    SinksFactory CreateDefaultSinksFactory(EventStoreName eventStore) =>
        @namespace => new Cratis.Chronicle.Storage.Sinks.Sinks(eventStore, @namespace, sinkFactories);
}
