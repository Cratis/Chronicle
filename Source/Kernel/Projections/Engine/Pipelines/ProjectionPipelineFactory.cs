// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Aksio.Cratis.Events.Store;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineFactory"/>.
/// </summary>
public class ProjectionPipelineFactory : IProjectionPipelineFactory
{
    readonly IProjectionSinks _projectionSinks;
    readonly IEventSequenceStorageProvider _eventProvider;
    readonly IObjectsComparer _objectsComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionPipelineFactory"/> class.
    /// </summary>
    /// <param name="projectionSinks"><see cref="IProjectionSinks"/> in the system.</param>
    /// <param name="eventProvider"><see cref="IEventSequenceStorageProvider"/> in the system.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ProjectionPipelineFactory(
        IProjectionSinks projectionSinks,
        IEventSequenceStorageProvider eventProvider,
        IObjectsComparer objectsComparer,
        IChangesetStorage changesetStorage,
        ILoggerFactory loggerFactory)
    {
        _projectionSinks = projectionSinks;
        _eventProvider = eventProvider;
        _objectsComparer = objectsComparer;
        _changesetStorage = changesetStorage;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IProjectionPipeline CreateFrom(IProjection projection, ProjectionPipelineDefinition definition)
    {
        // TODO: This should be taken out when we have the ImmediateProjection support.
        IProjectionSink sink = default!;
        if (definition.Sinks.Any())
        {
            var sinkDefinition = definition.Sinks.First();
            sink = _projectionSinks.GetForTypeAndModel(sinkDefinition.TypeId, projection.Model);
        }

        return new ProjectionPipeline(
            projection,
            _eventProvider,
            sink,
            _objectsComparer,
            _changesetStorage,
            _loggerFactory.CreateLogger<ProjectionPipeline>());
    }
}
