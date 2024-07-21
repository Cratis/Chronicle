// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Projections.Definitions;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionPipelineFactory"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for working with storage.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
/// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
public class ProjectionPipelineFactory(
    IStorage storage,
    IObjectComparer objectComparer,
    ITypeFormats typeFormats,
    ILoggerFactory loggerFactory) : IProjectionPipelineFactory
{
    /// <inheritdoc/>
    public IProjectionPipeline Create(
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EngineProjection projection,
        ProjectionDefinition definition)
    {
        var namespaceStorage = storage.GetEventStore(eventStore).GetNamespace(@namespace);
        var sink = namespaceStorage.Sinks.GetFor(definition.Sink.TypeId, projection.Model);

        return new ProjectionPipeline(
            projection,
            namespaceStorage.GetEventSequence(EventSequenceId.Log),
            sink,
            objectComparer,
            namespaceStorage.Changesets,
            typeFormats,
            loggerFactory.CreateLogger<ProjectionPipeline>());
    }
}
