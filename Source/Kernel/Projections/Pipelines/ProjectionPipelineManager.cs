// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Projections.Pipelines.Steps;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionPipelineManager"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
[Singleton]
public class ProjectionPipelineManager(
    IStorage storage,
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
        var sink = namespaceStorage.Sinks.GetFor(projection.Sink.TypeId, projection.Sink.ConfigurationId, projection.Model);

        IEnumerable<ICanPerformProjectionPipelineStep> steps =
        [
            new ResolveKey(eventSequenceStorage, typeFormats, loggerFactory.CreateLogger<ResolveKey>()),
            new SetInitialState(sink, loggerFactory.CreateLogger<SetInitialState>()),
            new HandleEvent(eventSequenceStorage, loggerFactory.CreateLogger<HandleEvent>()),
            new SaveChanges(sink, namespaceStorage.Changesets, loggerFactory.CreateLogger<SaveChanges>())
        ];

        return _pipelines[key] = new ProjectionPipeline(
            projection,
            sink,
            namespaceStorage.Changesets,
            objectComparer,
            steps,
            loggerFactory.CreateLogger<ProjectionPipeline>());
    }
}
