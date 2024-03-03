// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Grains.Projections.Pipelines;
using Cratis.Kernel.Projections;
using Cratis.Kernel.Projections.Pipelines;
using Cratis.Projections;
using Cratis.Projections.Definitions;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Kernel.Projections.IProjection;

namespace Cratis.Kernel.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionManager"/>.
/// </summary>
public class ProjectionManager : IProjectionManager
{
    readonly EventStoreName _eventStoreName;
    readonly EventStoreNamespaceName _eventStoreNamespace;
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    readonly ILogger<ProjectionManager> _logger;
    readonly Dictionary<ProjectionId, EngineProjection> _projections = new();
    readonly Dictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionManager"/> class.
    /// </summary>
    /// <param name="eventStoreName"><see cref="EventStoreName"/> the manager is for.</param>
    /// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the manager is for.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> to use.</param>
    /// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> to use.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ProjectionManager(
        EventStoreName eventStoreName,
        EventStoreNamespaceName eventStoreNamespace,
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory,
        ILogger<ProjectionManager> logger)
    {
        _eventStoreName = eventStoreName;
        _eventStoreNamespace = eventStoreNamespace;
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Exists(ProjectionId id) => _projections.ContainsKey(id);

    /// <inheritdoc/>
    public EngineProjection Get(ProjectionId id)
    {
        ThrowIfMissingProjection(id);
        return _projections[id];
    }

    /// <inheritdoc/>
    public IProjectionPipeline GetPipeline(ProjectionId id)
    {
        ThrowIfMissingProjection(id);
        return _pipelines[id];
    }

    /// <inheritdoc/>
    public async Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition)
    {
        using var scope = _logger.BeginProjectionManagerScope(_eventStoreName, _eventStoreNamespace);

        _logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);

        var projection = await _projectionFactory.CreateFrom(projectionDefinition);
        var pipeline = _projectionPipelineFactory.CreateFrom(projection, pipelineDefinition);

        _projections[projectionDefinition.Identifier] = projection;
        _pipelines[projectionDefinition.Identifier] = pipeline;
    }

    void ThrowIfMissingProjection(ProjectionId id)
    {
        if (!_projections.ContainsKey(id))
        {
            throw new MissingProjection(id);
        }
    }
}
