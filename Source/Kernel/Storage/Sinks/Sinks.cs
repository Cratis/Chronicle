// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Concepts.Sinks;
using Cratis.Chronicle.Storage.Graphs;
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
/// <param name="sinkConfigurations">The <see cref="ISinkConfigurations"/> resolving named sinks; none configured when omitted.</param>
public class Sinks(
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespaceName,
    IInstancesOf<ISinkFactory> sinkFactories,
    ISinkConfigurations? sinkConfigurations = null) : ISinks
{
    readonly Dictionary<SinkTypeId, ISinkFactory> _factories = sinkFactories.Where(_ => _ is not null).ToDictionary(_ => _.TypeId, _ => _);
    readonly ISinkConfigurations _configurations = sinkConfigurations ?? DefaultSinkConfigurations.Instance;
    readonly ConcurrentDictionary<SinkKey, SinkEntry> _sinks = new();
    readonly ConcurrentDictionary<SinkKey, SemaphoreSlim> _gates = new();
    readonly ConcurrentDictionary<SinkKey, GraphSinkEntry> _graphSinks = new();

    /// <inheritdoc/>
    public Task<ISink> GetFor(ReadModelDefinition readModel) => Get(readModel, initialize: true);

    /// <inheritdoc/>
    public Task<ISink> GetWithoutProvisioning(ReadModelDefinition readModel) => Get(readModel, initialize: false);

    /// <inheritdoc/>
    public Task<ISink> GetFor(ReadModelDefinition readModel, ReadModelContainerName container) =>
        GetFor(readModel with { ContainerName = container });

    /// <inheritdoc/>
    public Task<IGraphSink> GetGraphFor(ReadModelDefinition readModel)
    {
        var resolved = _configurations.ResolveFor(readModel, graph: true);
        ThrowIfUnknownSink(resolved.Type);
        if (_factories[resolved.Type] is not IGraphSinkFactory factory)
        {
            throw new SinkIsNotAGraphSink(resolved.Type);
        }

        var key = new SinkKey(resolved.Type, resolved.Name, readModel.Sink.Configuration, readModel.ContainerName, readModel.Sink.EventSequence);

        // A graph sink is built from the graph the read model declares, so a read model whose declaration changed - a node or an edge added, say -
        // needs a sink that knows the new graph. Serving the sink of the old one refuses every element the old graph did not have.
        return _graphSinks.AddOrUpdate(
            key,
            static (_, state) => GraphSinkEntry.For(state.Factory, state.EventStore, state.Namespace, state.ReadModel, state.Name),
            static (_, existing, state) => SameDefinition(existing.Definition, state.ReadModel) ? existing : GraphSinkEntry.For(state.Factory, state.EventStore, state.Namespace, state.ReadModel, state.Name),
            (Factory: factory, EventStore: eventStoreName, Namespace: eventStoreNamespaceName, ReadModel: readModel, resolved.Name)).Sink.Value;
    }

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
        var resolved = _configurations.ResolveFor(readModel, graph: false);
        ThrowIfUnknownSink(resolved.Type);
        var key = new SinkKey(resolved.Type, resolved.Name, readModel.Sink.Configuration, readModel.ContainerName, readModel.Sink.EventSequence);
        if (_sinks.TryGetValue(key, out var cached) && CanServe(cached, readModel, initialize)) return cached.Sink;
        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await GetUnderGate(key, resolved, readModel, initialize);
        }
        finally
        {
            gate.Release();
        }
    }

    async Task<ISink> GetUnderGate(SinkKey key, ResolvedSink resolved, ReadModelDefinition readModel, bool initialize)
    {
        _sinks.TryGetValue(key, out var existing);
        if (CanServe(existing, readModel, initialize)) return existing!.Sink;
        var sameDefinition = existing is not null && SameDefinition(existing.Definition, readModel);
        var sink = sameDefinition ? existing!.Sink : CreateSink(resolved, readModel);
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

    ISink CreateSink(ResolvedSink resolved, ReadModelDefinition readModel)
    {
        var factory = _factories[resolved.Type];
        return resolved.IsImplicitDefault
            ? factory.CreateFor(eventStoreName, eventStoreNamespaceName, readModel)
            : factory.CreateFor(eventStoreName, eventStoreNamespaceName, readModel, resolved.Name);
    }

    void ThrowIfUnknownSink(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkEntry(ISink Sink, ReadModelDefinition Definition, bool Initialized);

    sealed record GraphSinkEntry(Lazy<Task<IGraphSink>> Sink, ReadModelDefinition Definition)
    {
        internal static GraphSinkEntry For(IGraphSinkFactory factory, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName name) =>
            new(new(() => Create(factory, eventStore, @namespace, readModel, name)), readModel);

        static async Task<IGraphSink> Create(IGraphSinkFactory factory, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReadModelDefinition readModel, SinkName name)
        {
            var sink = name.IsDefault
                ? factory.CreateGraphFor(eventStore, @namespace, readModel)
                : factory.CreateGraphFor(eventStore, @namespace, readModel, name);
            await sink.EnsureSchema();
            return sink;
        }
    }

    sealed record SinkKey(SinkTypeId TypeId, SinkName Name, SinkConfigurationId ConfigurationId, ReadModelContainerName ContainerName, EventSequenceSinkConfiguration? EventSequence);
}
