// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Changes;
using Aksio.Cratis.Events.Projections.Changes;
using Aksio.Cratis.Events.Projections.Definitions;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Events.Projections.Pipelines;

/// <summary>
/// Represents an implementation of <see cref="IProjectionPipelineFactory"/>.
/// </summary>
public class ProjectionPipelineFactory : IProjectionPipelineFactory
{
    readonly IProjectionSinks _projectionSinks;
    readonly IProjectionEventProviders _projectionEventProviders;
    readonly IProjectionPositions _projectionPositions;
    readonly IObjectsComparer _objectsComparer;
    readonly IChangesetStorage _changesetStorage;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionPipelineFactory"/> class.
    /// </summary>
    /// <param name="projectionSinks"><see cref="IProjectionSinks"/> in the system.</param>
    /// <param name="projectionEventProviders"><see cref="IProjectionEventProviders"/> in the system.</param>
    /// <param name="projectionPositions"><see cref="IProjectionPositions"/> to use.</param>
    /// <param name="objectsComparer"><see cref="IObjectsComparer"/> for comparing objects.</param>
    /// <param name="changesetStorage"><see cref="IChangesetStorage"/> for storing changesets as they occur.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ProjectionPipelineFactory(
        IProjectionSinks projectionSinks,
        IProjectionEventProviders projectionEventProviders,
        IProjectionPositions projectionPositions,
        IObjectsComparer objectsComparer,
        IChangesetStorage changesetStorage,
        ILoggerFactory loggerFactory)
    {
        _projectionSinks = projectionSinks;
        _projectionEventProviders = projectionEventProviders;
        _projectionPositions = projectionPositions;
        _objectsComparer = objectsComparer;
        _changesetStorage = changesetStorage;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IProjectionPipeline CreateFrom(IProjection projection, ProjectionPipelineDefinition definition)
    {
        var eventProvider = _projectionEventProviders.GetForType(definition.ProjectionEventProviderTypeId);
        var handler = new ProjectionPipelineHandler(_objectsComparer, _changesetStorage, _projectionPositions, _loggerFactory.CreateLogger<ProjectionPipelineHandler>());
        var pipeline = new ProjectionPipeline(
            projection,
            eventProvider,
            handler,
            new ProjectionPipelineJobs(_projectionPositions, eventProvider, handler, _loggerFactory),
            _loggerFactory.CreateLogger<ProjectionPipeline>());

        foreach (var sinkDefinition in definition.Sinks)
        {
            var sink = _projectionSinks.GetForTypeAndModel(sinkDefinition.TypeId, projection.Model);
            pipeline.StoreIn(sinkDefinition.ConfigurationId, sink);
        }
        return pipeline;
    }
}
