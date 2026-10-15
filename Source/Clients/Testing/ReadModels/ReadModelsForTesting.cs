// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Sinks;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;
using Cratis.Json;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Represents a decorator around <see cref="IReadModels"/> for testing that intercepts
/// <c language="csharp">GetInstanceById</c> to return pre-seeded read model instances when available,
/// and delegates all other operations to the real inner implementation.
/// </summary>
/// <param name="inner">The real <see cref="IReadModels"/> implementation to delegate to.</param>
public class ReadModelsForTesting(IReadModels inner) : IReadModels
{
    readonly Dictionary<(string Identifier, string Key), object> _instances = [];
    readonly EventStoreForTesting? _store;
    readonly bool _foldReads;
    IEventSequence? _eventLog;

    /// <summary>
    /// Initializes a test read model surface that observes collections rebuilt from an event log.
    /// </summary>
    /// <param name="inner">The inner read model surface.</param>
    /// <param name="eventLog">The scenario event log to observe.</param>
    /// <remarks>
    /// Observation requires an inner testing surface with an owning store so that artifact discovery,
    /// services, and event storage match the supplied log. Otherwise Observe throws ReadModelCannotBeObserved.
    /// </remarks>
    public ReadModelsForTesting(IReadModels inner, IEventSequence eventLog)
        : this(inner)
    {
        _eventLog = eventLog;
        _store = (inner as ReadModelsForTesting)?._store;
    }

    /// <summary>
    /// Initializes observation using the owning scenario's discovered artifacts and services.
    /// </summary>
    /// <param name="inner">The inner read model surface.</param>
    /// <param name="eventLog">The event log.</param>
    /// <param name="store">The owning scenario store.</param>
    /// <param name="foldReads">Whether snapshot reads rebuild from the scenario's own log.</param>
    internal ReadModelsForTesting(IReadModels inner, IEventSequence eventLog, EventStoreForTesting store, bool foldReads = false)
        : this(inner, eventLog)
    {
        _store = store;
        _foldReads = foldReads;
    }

    /// <summary>
    /// Gets the <see cref="IMaterializedReadModels"/> for working with materialized read model instances.
    /// </summary>
    public IMaterializedReadModels Materialized => inner.Materialized;

    /// <inheritdoc/>
    public Task Register() => inner.Register();

    /// <inheritdoc/>
    public Task Register<TReadModel>() => inner.Register<TReadModel>();

    /// <inheritdoc/>
    /// <remarks>
    /// Nothing seeded under this key - whether because the id was never seeded at all, or a different id
    /// was - resolves to null rather than falling through to the inner IReadModels, which needs a grain and
    /// throws GrainNotAvailableInTestScenario in-process. This matches what the interface's nullable return
    /// already promises for "the instance does not exist" (#3927).
    /// </remarks>
    public async Task<TReadModel> GetInstanceById<TReadModel>(ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        ReadProtectionGuard.Read(nameof(GetInstanceById));
        var instance = await GetInstanceById(typeof(TReadModel), key, sessionId);
        return instance is null ? default! : (TReadModel)instance;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// See the generic overload above - the same reasoning applies here (#3927).
    /// </remarks>
    public async Task<object> GetInstanceById(Type readModelType, ReadModelKey key, ReadModelSessionId? sessionId = null)
    {
        ReadProtectionGuard.Read(nameof(GetInstanceById));
        EventStreamReadModel.RequireStreamScope(readModelType);
        var identifier = readModelType.GetReadModelIdentifier();
        if (_instances.TryGetValue((identifier, key.Value), out var instance)) return instance;
        if (!_foldReads) return null!;

        var instances = await Rebuild(readModelType, _eventLog!, _store!);
        using var sink = ProjectionReadModelProcessor.CreateSink(readModelType, _store!.JsonSchemaGenerator);
        var canonicalKey = sink.GetKeyValue(new(key.Value, ArrayIndexers.NoIndexers));
        return instances.TryGetValue(canonicalKey, out instance) ? instance : null!;
    }

    /// <inheritdoc/>
    public async Task<TReadModel> GetInstanceById<TReadModel>(EventSourceId eventSourceId, EventStreamId eventStreamId)
    {
        ReadProtectionGuard.Read(nameof(GetInstanceById));
        if (_store is null) return await inner.GetInstanceById<TReadModel>(eventSourceId, eventStreamId);
        var scope = EventStreamReadModel.ScopeFor(typeof(TReadModel), _store.EventSources, eventSourceId, eventStreamId);
        var events = await (_eventLog ?? _store.EventLog).GetFromSequenceNumber(EventSequenceNumber.First);
        var source = new EventSourceId(scope.EventSourceId.Value);
        var selected = events.Where(@event =>
            @event.Context.EventSourceId.Value == source.Value &&
            @event.Context.EventSourceType == scope.EventSourceType &&
            @event.Context.EventStreamType == scope.EventStreamType &&
            (scope.EventStreamId.IsDefault || @event.Context.EventStreamId == scope.EventStreamId));
        var instances = await ReadModelFolder.Fold(_store, typeof(TReadModel), selected);
        return instances.TryGetValue(source, out var instance) ? (TReadModel)instance : default!;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TReadModel>> GetInstances<TReadModel>(EventCount? eventCount = null)
    {
        ReadProtectionGuard.Read(nameof(GetInstances));
        EventStreamReadModel.RequireStreamScope(typeof(TReadModel));
        if (!_foldReads) return await inner.GetInstances<TReadModel>(eventCount);

        var instances = await Rebuild(typeof(TReadModel), _eventLog!, _store!, eventCount);
        return instances.Values.Cast<TReadModel>().ToArray();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<TReadModel>> Observe<TReadModel>(Expression<Func<TReadModel, bool>>? filter = null)
    {
        ReadProtectionGuard.Read(nameof(Observe));
        EventStreamReadModel.RequireStreamScope(typeof(TReadModel));
        var log = _eventLog ?? throw new ReadModelCannotBeObserved(typeof(TReadModel), "An event log is not available in this test scenario");
        var metadata = StoreForObservation(typeof(TReadModel));
        var predicate = filter?.Compile();
        var source = Observable.Create<IEnumerable<TReadModel>>(observer =>
        {
            var signals = new Subject<Unit>();
            var rebuilds = signals.Select(_ => Observable.FromAsync(() => Rebuild<TReadModel>(log, metadata)))
                .Concat()
                .Select(set => (IEnumerable<TReadModel>)(predicate is null ? set.ToArray() : set.Where(predicate).ToArray()))
                .DistinctUntilChanged(set => JsonSerializer.Serialize(set, Globals.JsonSerializerOptions))
                .Subscribe(observer);
            var appends = log.AppendOperations.Where(batch => batch.Any(append => append.Result.IsSuccess))
                .Subscribe(_ => signals.OnNext(Unit.Default), signals.OnError, signals.OnCompleted);
            signals.OnNext(Unit.Default);
            return new CompositeDisposable(appends, rebuilds, signals);
        });
        return new ReadModelObservation<TReadModel>(source);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ReadModelSnapshot<TReadModel>>> GetSnapshotsById<TReadModel>(ReadModelKey readModelKey)
    {
        ReadProtectionGuard.Read(nameof(GetSnapshotsById));
        EventStreamReadModel.RequireStreamScope(typeof(TReadModel));
        return inner.GetSnapshotsById<TReadModel>(readModelKey);
    }

    /// <inheritdoc/>
    public IObservable<ReadModelChangeset<TReadModel>> Watch<TReadModel>()
    {
        ReadProtectionGuard.Read(nameof(Watch));
        EventStreamReadModel.RequireStreamScope(typeof(TReadModel));
        return inner.Watch<TReadModel>();
    }

    /// <inheritdoc/>
    public IReadModelWatcher<TReadModel> GetWatcherFor<TReadModel>()
    {
        ReadProtectionGuard.Read(nameof(GetWatcherFor));
        EventStreamReadModel.RequireStreamScope(typeof(TReadModel));
        return inner.GetWatcherFor<TReadModel>();
    }

    /// <inheritdoc/>
    public Task DehydrateSession(ReadModelSessionId sessionId, Type readModelType, ReadModelKey readModelKey) =>
        inner.DehydrateSession(sessionId, readModelType, readModelKey);

    /// <inheritdoc/>
    public Task<TReadModel> Release<TReadModel>(TReadModel instance) =>
        inner.Release(instance);

    /// <inheritdoc/>
    public Task<IEnumerable<TReadModel>> Release<TReadModel>(IEnumerable<TReadModel> instances) =>
        inner.Release(instances);

    /// <summary>
    /// Keeps the ReadModelScenario surface unsupported: its collected events are not appended to the log.
    /// </summary>
    internal void DisableObservation() => _eventLog = null;

    /// <summary>
    /// Gets the keys of the instances seeded for a read model type.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <returns>The keys with seeded instances.</returns>
    internal IEnumerable<ReadModelKey> GetSeededKeys(Type readModelType)
    {
        var identifier = readModelType.GetReadModelIdentifier();
        return _instances.Keys.Where(key => key.Identifier == identifier).Select(key => new ReadModelKey(key.Key));
    }

    /// <summary>Gets a pre-seeded model instance, if present.</summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="key">The model key.</param>
    /// <returns>The seeded model or null.</returns>
    internal TReadModel? GetSeededInstance<TReadModel>(ReadModelKey key)
        where TReadModel : class =>
        _instances.TryGetValue((typeof(TReadModel).GetReadModelIdentifier(), key.Value), out var instance) ? (TReadModel)instance : null;

    /// <summary>
    /// Registers a pre-seeded read model instance so that subsequent <c language="csharp">GetInstanceById</c> calls
    /// return it directly without hitting the server.
    /// </summary>
    /// <typeparam name="TReadModel">The type of read model to register.</typeparam>
    /// <param name="eventSourceId">The event source identifier to associate with the read model.</param>
    /// <param name="instance">The read model instance to pre-seed.</param>
    internal void RegisterInstance<TReadModel>(EventSourceId eventSourceId, TReadModel instance)
        where TReadModel : class
    {
        var identifier = typeof(TReadModel).GetReadModelIdentifier();
        _instances[(identifier, eventSourceId.Value)] = instance;
    }

    static bool IsPassive(Type readModelType, EventStoreForTesting store)
    {
        if (readModelType.IsPassive()) return true;
        return store.Projections is IKnowPassiveProjections passive && passive.IsPassive(readModelType);
    }

    EventStoreForTesting StoreForObservation(Type readModelType)
    {
        var store = _store ?? throw new ReadModelCannotBeObserved(readModelType, "The owning test store is not available for artifact discovery and event storage");
        if (!store.Projections.HasFor(readModelType) && !store.Reducers.HasFor(readModelType))
        {
            throw new UnknownReadModel(readModelType);
        }

        if (IsPassive(readModelType, store))
        {
            throw new ReadModelCannotBeObserved(readModelType, "Passive read models have no materialized sink");
        }

        if (readModelType.IsEventTypeTarget())
        {
            throw new ReadModelCannotBeObserved(readModelType, "Event targets publish events instead of storing a collection");
        }

        return store;
    }

    async Task<IEnumerable<TReadModel>> Rebuild<TReadModel>(IEventSequence log, EventStoreForTesting store)
    {
        var instances = await Rebuild(typeof(TReadModel), log, store);
        return instances.Values.Cast<TReadModel>().ToArray();
    }

    async Task<IReadOnlyDictionary<object, object>> Rebuild(Type readModelType, IEventSequence log, EventStoreForTesting store, EventCount? eventCount = null)
    {
        if (!store.Projections.HasFor(readModelType) && !store.Reducers.HasFor(readModelType)) throw new UnknownReadModel(readModelType);

        var events = await log.GetFromSequenceNumber(EventSequenceNumber.First);
        var selectedEvents = eventCount is null || eventCount == EventCount.Unlimited
            ? events
            : events.Where((_, index) => (ulong)index < eventCount.Value);
        var folded = await ReadModelFolder.Fold(store, readModelType, selectedEvents);
        var identifier = readModelType.GetReadModelIdentifier().Value;
        using var sink = ProjectionReadModelProcessor.CreateSink(readModelType, store.JsonSchemaGenerator);
        var instances = new Dictionary<object, object>();
        foreach (var pair in _instances.Where(pair => pair.Key.Identifier == identifier))
        {
            instances[sink.GetKeyValue(new(pair.Key.Key, ArrayIndexers.NoIndexers))] = pair.Value;
        }

        foreach (var (key, instance) in folded)
        {
            instances[sink.GetKeyValue(new(key.Value, ArrayIndexers.NoIndexers))] = instance;
        }

        return instances;
    }
}
