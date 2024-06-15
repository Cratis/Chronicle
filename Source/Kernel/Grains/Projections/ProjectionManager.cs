// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Grains.Projections.Pipelines;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Projections;
using Cratis.Projections.Definitions;
using Microsoft.Extensions.Logging;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionManager"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionManager"/> class.
/// </remarks>
/// <param name="eventStoreName"><see cref="EventStoreName"/> the manager is for.</param>
/// <param name="eventStoreNamespace"><see cref="EventStoreNamespaceName"/> the manager is for.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> to use.</param>
/// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> to use.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ProjectionManager(
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespace,
    IProjectionFactory projectionFactory,
    IProjectionPipelineFactory projectionPipelineFactory,
    ILogger<ProjectionManager> logger) : IProjectionManager
{
    readonly Dictionary<ProjectionId, EngineProjection> _projections = [];
    readonly Dictionary<ProjectionId, IProjectionPipeline> _pipelines = [];

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
        using var scope = logger.BeginProjectionManagerScope(eventStoreName, eventStoreNamespace);

        logger.Registering(projectionDefinition.Identifier, projectionDefinition.Name);

        var projection = await projectionFactory.CreateFrom(projectionDefinition);
        var pipeline = projectionPipelineFactory.CreateFrom(projection, pipelineDefinition);

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
