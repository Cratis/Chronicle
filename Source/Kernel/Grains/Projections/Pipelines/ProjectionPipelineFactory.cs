// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Changes;
using Cratis.EventSequences;
using Cratis.Kernel.Projections.Pipelines;
using Cratis.Kernel.Storage;
using Cratis.Kernel.Storage.Sinks;
using Cratis.Projections.Definitions;
using Cratis.Schemas;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Kernel.Projections.IProjection;

namespace Cratis.Kernel.Grains.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineFactory"/>.
/// </summary>
public class ProjectionPipelineFactory : IProjectionPipelineFactory
{
    readonly ISinks _sinks;
    readonly IEventStoreNamespaceStorage _storage;
    readonly IObjectComparer _objectComparer;
    readonly ITypeFormats _typeFormats;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionPipelineFactory"/> class.
    /// </summary>
    /// <param name="sinks"><see cref="ISinks"/> in the system.</param>
    /// <param name="storage"><see cref="IEventStoreNamespaceStorage"/> for accessing underlying storage for the specific namespace.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ProjectionPipelineFactory(
        ISinks sinks,
        IEventStoreNamespaceStorage storage,
        IObjectComparer objectComparer,
        ITypeFormats typeFormats,
        ILoggerFactory loggerFactory)
    {
        _sinks = sinks;
        _storage = storage;
        _objectComparer = objectComparer;
        _typeFormats = typeFormats;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IProjectionPipeline CreateFrom(EngineProjection projection, ProjectionPipelineDefinition definition)
    {
        ISink sink = default!;
        if (definition.Sinks.Any())
        {
            var sinkDefinition = definition.Sinks.First();
            sink = _sinks.GetFor(sinkDefinition.TypeId, projection.Model);
        }

        return new ProjectionPipeline(
            projection,
            _storage.GetEventSequence(EventSequenceId.Log),
            sink,
            _objectComparer,
            _storage.Changesets,
            _typeFormats,
            _loggerFactory.CreateLogger<ProjectionPipeline>());
    }
}
