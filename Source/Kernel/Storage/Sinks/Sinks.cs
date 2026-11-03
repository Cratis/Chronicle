// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Types;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Sinks"/> class.
/// </remarks>
/// <param name="eventStoreName"><see cref="EventStoreName"/> the <see cref="Sinks"/> are for.</param>
/// <param name="eventStoreNamespaceName"><see cref="EventStoreNamespaceName"/> the <see cref="Sinks"/> are for.</param>
/// <param name="sinkFactories"><see cref="IInstancesOf{T}"/> of <see cref="ISinkFactory"/>.</param>
public class Sinks(
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespaceName,
    IInstancesOf<ISinkFactory> sinkFactories) : ISinks
{
    readonly Dictionary<SinkTypeId, ISinkFactory> _factories = sinkFactories.Where(_ => _ is not null).ToDictionary(_ => _.TypeId, _ => _);
    readonly ConcurrentDictionary<SinkKey, SinkEntry> _sinks = new();
    readonly ConcurrentDictionary<SinkKey, SemaphoreSlim> _gates = new();

    /// <inheritdoc/>
    public Task<ISink> GetFor(ReadModelDefinition readModel) => Get(readModel, initialize: true);

    /// <inheritdoc/>
    public Task<ISink> GetWithoutProvisioning(ReadModelDefinition readModel) => Get(readModel, initialize: false);

    /// <inheritdoc/>
    public Task<ISink> GetFor(ReadModelDefinition readModel, ReadModelContainerName container) =>
        GetFor(readModel with { ContainerName = container });

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    static async Task Initialize(ISink sink, bool changed)
    {
        if (changed && sink is ICanReconcileIndexes reconciler)
        {
            await reconciler.ReconcileIndexes();
        }
        else
        {
            await sink.EnsureIndexes();
        }
    }

    static bool SameDefinition(ReadModelDefinition first, ReadModelDefinition second) =>
        first.Schemas.Count == second.Schemas.Count &&
        first.Schemas.All(pair => second.Schemas.TryGetValue(pair.Key, out var schema) && pair.Value.ToJson() == schema.ToJson()) &&
        first.Indexes.SequenceEqual(second.Indexes);

    static bool CanServe(SinkEntry? entry, ReadModelDefinition readModel, bool initialize) =>
        entry is not null && SameDefinition(entry.Definition, readModel) && (entry.Initialized || !initialize);

    async Task<ISink> Get(ReadModelDefinition readModel, bool initialize)
    {
        readModel.Sink.EnsureReadModelSupported();
        ThrowIfUnknownSink(readModel.Sink.Type);
        var key = new SinkKey(readModel.Sink.Type, readModel.Sink.Configuration, readModel.ContainerName, readModel.Sink.EventSequence);
        if (_sinks.TryGetValue(key, out var cached) && CanServe(cached, readModel, initialize)) return cached.Sink;
        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await GetUnderGate(key, readModel, initialize);
        }
        finally
        {
            gate.Release();
        }
    }

    async Task<ISink> GetUnderGate(SinkKey key, ReadModelDefinition readModel, bool initialize)
    {
        _sinks.TryGetValue(key, out var existing);
        if (CanServe(existing, readModel, initialize)) return existing!.Sink;
        var sameDefinition = existing is not null && SameDefinition(existing.Definition, readModel);
        var sink = sameDefinition ? existing!.Sink : _factories[readModel.Sink.Type].CreateFor(eventStoreName, eventStoreNamespaceName, readModel);
        if (initialize)
        {
            await Initialize(sink, existing is not null && !sameDefinition);
        }

        // A sink for a changed definition is never cached uninitialized: the next initializing request must reconcile its indexes.
        if (initialize || existing is null)
        {
            _sinks[key] = new(sink, readModel, initialize);
        }

        return sink;
    }

    void ThrowIfUnknownSink(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkEntry(ISink Sink, ReadModelDefinition Definition, bool Initialized);

    sealed record SinkKey(SinkTypeId TypeId, SinkConfigurationId ConfigurationId, ReadModelContainerName ContainerName, EventSequenceSinkConfiguration? EventSequence);
}
