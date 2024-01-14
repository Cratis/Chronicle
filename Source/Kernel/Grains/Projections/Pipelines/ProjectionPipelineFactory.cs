// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Kernel.Storage;
using Aksio.Cratis.Kernel.Storage.Sinks;
using Aksio.Cratis.Projections.Definitions;
using Aksio.Cratis.Schemas;
using Microsoft.Extensions.Logging;
using EngineProjection = Aksio.Cratis.Kernel.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections.Pipelines;

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
