// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections.Engine.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineManager"/>.
/// </summary>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="projectionFutures"><see cref="IProjectionFutures"/> for managing projection futures.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Singleton]
public class ProjectionPipelineManager(
    IStorage storage,
    IProjectionFutures projectionFutures,
    IObjectComparer objectComparer,
    ITypeFormats typeFormats,
    ILoggerFactory loggerFactory) : IProjectionPipelineManager
{
    readonly ConcurrentDictionary<string, IProjectionPipeline> _pipelines = new();

    /// <inheritdoc/>
    public IProjectionPipeline GetFor(
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
        var eventSequenceStorage = namespaceStorage.GetEventSequence(projection.EventSequenceId);
        var sink = namespaceStorage.Sinks.GetFor(projection.ReadModel);

        var resolveFuturesStep = new ResolveFutures(eventStore, @namespace, projectionFutures, typeFormats, loggerFactory.CreateLogger<ResolveFutures>());

        IEnumerable<ICanPerformProjectionPipelineStep> steps =
        [
            new ResolveKey(eventSequenceStorage, sink, typeFormats, loggerFactory.CreateLogger<ResolveKey>()),
            new SetInitialState(sink, loggerFactory.CreateLogger<SetInitialState>()),
            new HandleEvent(eventSequenceStorage, sink, loggerFactory.CreateLogger<HandleEvent>()),
            new StoreFutures(eventStore, @namespace, projectionFutures, loggerFactory.CreateLogger<StoreFutures>()),
            resolveFuturesStep,
            new SaveChanges(sink, namespaceStorage.Changesets, loggerFactory.CreateLogger<SaveChanges>())
        ];

        var newPipeline = new ProjectionPipeline(
            projection,
            sink,
            namespaceStorage.Changesets,
            objectComparer,
            steps,
            loggerFactory.CreateLogger<ProjectionPipeline>());

        return _pipelines[key] = newPipeline;
    }

    /// <inheritdoc/>
    public void EvictFor(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id) =>
        _pipelines.TryRemove(KeyHelper.Combine(eventStore, @namespace, id), out _);
}
