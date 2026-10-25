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
    readonly ConcurrentDictionary<SinkKey, (ISink Sink, ReadModelDefinition Definition)> _sinks = new();
    readonly ConcurrentDictionary<SinkKey, SemaphoreSlim> _gates = new();
    readonly ConcurrentDictionary<GenerationSinkContext, Lazy<Task<ISink>>> _generationSinks = new();

    /// <inheritdoc/>
    public async Task<ISink> GetFor(GenerationSinkContext context)
    {
        var model = context.GetReadModel(eventStoreName, eventStoreNamespaceName);
        var pending = _generationSinks.GetOrAdd(context, static (key, state) => new Lazy<Task<ISink>>(() => state.Owner.CreateGenerationSink(key, state.Model)), (Owner: this, Model: model));
        try
        {
            var sink = await pending.Value;
            EvictSuperseded(context);
            return sink;
        }
        catch
        {
            _generationSinks.TryRemove(new KeyValuePair<GenerationSinkContext, Lazy<Task<ISink>>>(context, pending));
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ISink> GetFor(ReadModelDefinition readModel)
    {
        readModel.Sink.EnsureReadModelSupported();
        ThrowIfUnknownSink(readModel.Sink.Type);
        var key = new SinkKey(readModel.Sink.Type, readModel.Sink.Configuration, readModel.ContainerName, readModel.Sink.EventSequence);
        if (_sinks.TryGetValue(key, out var cached) && SameDefinition(cached.Definition, readModel)) return cached.Sink;
        var gate = _gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var changed = _sinks.TryGetValue(key, out var existing);
            if (changed && SameDefinition(existing.Definition, readModel)) return existing.Sink;
            var sink = _factories[readModel.Sink.Type].CreateFor(eventStoreName, eventStoreNamespaceName, readModel);
            if (changed && sink is ICanReconcileIndexes reconciler)
            {
                await reconciler.ReconcileIndexes();
            }
            else
            {
                await sink.EnsureIndexes();
            }
            _sinks[key] = (sink, readModel);
            return sink;
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc/>
    public Task<ISink> GetFor(ReadModelDefinition readModel, ReadModelContainerName container) =>
        GetFor(readModel with { ContainerName = container });

    /// <inheritdoc/>
    public bool HasType(SinkTypeId typeId) => _factories.ContainsKey(typeId);

    static bool SameDefinition(ReadModelDefinition first, ReadModelDefinition second) =>
        first.Schemas.Count == second.Schemas.Count &&
        first.Schemas.All(pair => second.Schemas.TryGetValue(pair.Key, out var schema) && pair.Value.ToJson() == schema.ToJson()) &&
        first.Indexes.SequenceEqual(second.Indexes);

    void EvictSuperseded(GenerationSinkContext context)
    {
        // Cache entries only: a superseded sink is never disposed or cleared here.
        foreach (var other in _generationSinks.Keys.Where(_ => _.Grant.Store == context.Grant.Store && _.Binding.Key == context.Binding.Key && _.Binding.Identity == context.Binding.Identity && _.Grant.Epoch < context.Grant.Epoch))
            _generationSinks.TryRemove(other, out _);
    }

    async Task<ISink> CreateGenerationSink(GenerationSinkContext context, ReadModelDefinition readModel)
    {
        if (!_factories.TryGetValue(readModel.Sink.Type, out var factory)) throw new UnknownSink(readModel.Sink.Type);
        var sink = factory.CreateFor(eventStoreName, eventStoreNamespaceName, context);
        await sink.EnsureIndexes();
        return sink;
    }

    void ThrowIfUnknownSink(SinkTypeId typeId)
    {
        if (!HasType(typeId)) throw new UnknownSink(typeId);
    }

    sealed record SinkKey(SinkTypeId TypeId, SinkConfigurationId ConfigurationId, ReadModelContainerName ContainerName, EventSequenceSinkConfiguration? EventSequence);
}
