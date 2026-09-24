// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineManager"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting PII fields.</param>
/// <param name="options">The <see cref="ChronicleOptions"/> holding the read model write configuration.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Singleton]
public class ProjectionPipelineManager(
    IStorage storage,
    IGrainFactory grainFactory,
    IObjectComparer objectComparer,
    ITypeFormats typeFormats,
    IReadModelsCompliance readModelsCompliance,
    IOptions<ChronicleOptions> options,
    ILoggerFactory loggerFactory) : IProjectionPipelineManager
{
    readonly ConcurrentDictionary<string, IProjectionPipeline> _pipelines = new();

    /// <summary>
    /// Per-projection handle lock keyed by (eventStore, namespace, projectionId).
    /// </summary>
    /// <remarks>
    /// Survives pipeline cache eviction (which happens on every Replay) so concurrent Handle
    /// calls across an old pipeline (still held by an already-activated subscriber) and a
    /// new pipeline (the one EvictFor just created for the replay) still serialize on the
    /// same lock. Without this, the read-modify-write cycle in SetInitialState → HandleEvent
    /// → SaveChanges races for hierarchical or join projections, leaving parent/child links
    /// empty. Each lock stripes purely event-source-keyed projections per event source id and
    /// serializes all other projections coarsely; growth is bounded by projection count.
    /// </remarks>
    readonly ConcurrentDictionary<string, ProjectionHandleLock> _handleLocks = new();

    /// <inheritdoc/>
    public async Task<IProjectionPipeline> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EngineProjection projection)
    {
        var key = KeyHelper.Combine(eventStore, @namespace, projection.Identifier);
        if (_pipelines.TryGetValue(key, out var pipeline))
        {
            return pipeline;
        }

        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var replayScopedStorage = new ReplayScopedEventSequenceStorage(namespaceStorage.GetEventSequence(projection.EventSequenceId));

        // A projection is observed per namespace whatever its scope - the events it reacts to only exist there.
        // The scope decides where the result lands. A globally scoped projection resolves its sink against the
        // NotSet namespace, the same event-store-level sentinel event seeding uses, so every namespace's observer
        // accumulates into a single instance. Everything else stays namespaced, which is the default and the
        // behavior every existing projection already has.
        var sinkStorage = projection.Scope == ProjectionScope.Global
            ? storage.GetEventStore(eventStore).GetNamespace(EventStoreNamespaceName.NotSet)
            : namespaceStorage;
        var sink = await sinkStorage.Sinks.GetFor(projection.ReadModel);

        var projectionFutures = grainFactory.GetProjectionFutures(eventStore, @namespace, projection.Identifier);
        var futuresTracker = new ProjectionFuturesTracker();
        var resolveFuturesStep = new ResolveFutures(projectionFutures, futuresTracker, typeFormats, objectComparer, loggerFactory.CreateLogger<ResolveFutures>());

        IEnumerable<ICanPerformProjectionPipelineStep> steps =
        [
            new ResolveKey(replayScopedStorage, sink, typeFormats, loggerFactory.CreateLogger<ResolveKey>()),
            new SetInitialState(sink, loggerFactory.CreateLogger<SetInitialState>()),
            new DecryptInitialState(readModelsCompliance, eventStore, @namespace),
            new HandleEvent(replayScopedStorage, sink, loggerFactory.CreateLogger<HandleEvent>()),
            new EncryptChangeset(readModelsCompliance, objectComparer, eventStore, @namespace),
            new StoreFutures(projectionFutures, futuresTracker, loggerFactory.CreateLogger<StoreFutures>()),
            resolveFuturesStep,
            new SaveChanges(sink, namespaceStorage.Changesets, options.Value.ReadModels.GuardSinkWritesOnWatermark, loggerFactory.CreateLogger<SaveChanges>())
        ];

        var handleLock = _handleLocks.GetOrAdd(key, _ => new ProjectionHandleLock());

        var newPipeline = new ProjectionPipeline(
            projection,
            sink,
            namespaceStorage.Changesets,
            objectComparer,
            steps,
            handleLock,
            replayScopedStorage,
            loggerFactory.CreateLogger<ProjectionPipeline>());

        return _pipelines[key] = newPipeline;
    }

    /// <inheritdoc/>
    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id) =>
        _pipelines.TryRemove(KeyHelper.Combine(eventStore, @namespace, id), out _);

    /// <inheritdoc/>
    public void Clear() => _pipelines.Clear();
}
