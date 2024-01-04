// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Kernel.Storage.Changes;
using Aksio.Cratis.Kernel.Storage.EventSequences;
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
    readonly IEventSequenceStorage _eventSequenceStorage;
    readonly IObjectComparer _objectComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ITypeFormats _typeFormats;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionPipelineFactory"/> class.
    /// </summary>
    /// <param name="sinks"><see cref="ISinks"/> in the system.</param>
    /// <param name="eventSequenceStorage"><see cref="IEventSequenceStorage"/> in the system.</param>
    /// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="typeFormats"><see cref="ITypeFormats"/> for resolving actual CLR types for schemas.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ProjectionPipelineFactory(
        ISinks sinks,
        IEventSequenceStorage eventSequenceStorage,
        IObjectComparer objectComparer,
        IChangesetStorage changesetStorage,
        ITypeFormats typeFormats,
        ILoggerFactory loggerFactory)
    {
        _sinks = sinks;
        _eventSequenceStorage = eventSequenceStorage;
        _objectComparer = objectComparer;
        _changesetStorage = changesetStorage;
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
            sink = _sinks.GetForTypeAndModel(sinkDefinition.TypeId, projection.Model);
        }

        return new ProjectionPipeline(
            projection,
            _eventSequenceStorage,
            sink,
            _objectComparer,
            _changesetStorage,
            _typeFormats,
            _loggerFactory.CreateLogger<ProjectionPipeline>());
    }
}
