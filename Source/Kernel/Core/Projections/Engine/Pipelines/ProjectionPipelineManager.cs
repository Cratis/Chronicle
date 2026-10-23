// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Generations;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Projections.Generations;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
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
/// <param name="readModelsCompliance">The <see cref="IReadModelsCompliance"/> for encrypting and decrypting compliance and security fields.</param>
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
    readonly ConcurrentDictionary<string, CachedPipeline> _pipelines = new();

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
        if (_pipelines.TryGetValue(key, out var pipeline) && pipeline.Fingerprint == projection.DefinitionFingerprint)
        {
            return pipeline.Pipeline;
        }

        pipeline?.Pipeline.ClearPreparedPage();
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);

        // A projection is observed per namespace whatever its scope - the events it reacts to only exist there.
        // The scope decides where the result lands. A globally scoped projection resolves its sink against the
        // NotSet namespace, the same event-store-level sentinel event seeding uses, so every namespace's observer
        // accumulates into a single instance. Everything else stays namespaced, which is the default and the
        // behavior every existing projection already has.
        var sinkStorage = projection.Scope == ProjectionScope.Global
            ? storage.GetEventStore(eventStore).GetNamespace(EventStoreNamespaceName.NotSet)
            : namespaceStorage;

        // Sinks owns the sink cache and its replay/bulk mode. Replacing a pipeline must reuse
        // that sink rather than begin replay again (which would clear already replayed data).
        var sink = await sinkStorage.Sinks.GetFor(projection.ReadModel);
        var projectionFutures = grainFactory.GetProjectionFutures(eventStore, @namespace, projection.Identifier);
        var newPipeline = Build(eventStore, @namespace, projection, namespaceStorage, sink, projectionFutures, key, null);
        _pipelines[key] = new(newPipeline, projection.DefinitionFingerprint);
        return newPipeline;
    }

    /// <inheritdoc/>
    public async Task<IProjectionPipeline> GetFor(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EngineProjection projection,
        MaterializationGenerationIdentity generation)
    {
        MaterializationGenerationCapabilities.ThrowIfCannotBuild(projection, generation);
        var key = GenerationKey(eventStore, @namespace, generation);
        if (_pipelines.TryGetValue(key, out var pipeline) && pipeline.Fingerprint == projection.DefinitionFingerprint)
        {
            return pipeline.Pipeline;
        }

        pipeline?.Pipeline.ClearPreparedPage();
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);

        // The generation addresses its destination explicitly; it never goes through the read model's own container
        // or its sink's replay mode, so two generations of one read model never share a sink, a mode or a cache entry.
        var sink = await namespaceStorage.Sinks.GetFor(projection.ReadModel, generation.Destination);

        // Futures, like observer state, belong to the generation's observer. Generation 1's observer is the projection
        // itself, so its futures stay where they always were.
        var projectionFutures = grainFactory.GetProjectionFutures(eventStore, @namespace, generation.ObserverId);
        var newPipeline = Build(eventStore, @namespace, projection, namespaceStorage, sink, projectionFutures, key, generation);
        _pipelines[key] = new(newPipeline, projection.DefinitionFingerprint);
        return newPipeline;
    }

    /// <inheritdoc/>
    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id) =>
        _pipelines.TryRemove(KeyHelper.Combine(eventStore, @namespace, id), out _);

    /// <inheritdoc/>
    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationIdentity generation) =>
        Evict(GenerationKey(eventStore, @namespace, generation));

    /// <inheritdoc/>
    public void Clear() => _pipelines.Clear();

    /// <summary>
    /// Gets the cache and handle lock key of a generation.
    /// </summary>
    /// <remarks>
    /// Generation 1 writes the projection's own container, so it shares the key - and with it the pipeline and the handle
    /// lock - with the projection. Every later generation is keyed by projection and generation explicitly, never by its
    /// observer identifier, so it cannot collide with a projection that happens to carry that identifier.
    /// </remarks>
    /// <param name="eventStore">The <see cref="EventStoreName"/>.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <param name="generation">The <see cref="MaterializationGenerationIdentity"/>.</param>
    /// <returns>The key.</returns>
    static string GenerationKey(EventStoreName eventStore, EventStoreNamespaceName @namespace, MaterializationGenerationIdentity generation) =>
        generation.Generation.IsFirst
            ? KeyHelper.Combine(eventStore, @namespace, generation.ProjectionId)
            : KeyHelper.Combine(eventStore, @namespace, generation.ProjectionId, generation.Generation);

    void Evict(string key)
    {
        if (_pipelines.TryRemove(key, out var pipeline)) pipeline.Pipeline.ClearPreparedPage();
    }

    ProjectionPipeline Build(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EngineProjection projection,
        IEventStoreNamespaceStorage namespaceStorage,
        ISink sink,
        IProjectionFutures projectionFutures,
        string key,
        MaterializationGenerationIdentity? generation)
    {
        var replayScopedStorage = new ReplayScopedEventSequenceStorage(namespaceStorage.GetEventSequence(projection.EventSequenceId));
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
            new SaveChanges(sink, namespaceStorage.Changesets, options.Value.ReadModels.GuardSinkWritesOnWatermark, loggerFactory.CreateLogger<SaveChanges>(), projectionFutures, generation?.Destination)
        ];

        var handleLock = _handleLocks.GetOrAdd(key, _ => new ProjectionHandleLock());

        return new ProjectionPipeline(
            projection,
            sink,
            namespaceStorage.Changesets,
            objectComparer,
            steps,
            handleLock,
            replayScopedStorage,
            loggerFactory.CreateLogger<ProjectionPipeline>(),
            generation);
    }

    sealed record CachedPipeline(IProjectionPipeline Pipeline, string Fingerprint);
}
