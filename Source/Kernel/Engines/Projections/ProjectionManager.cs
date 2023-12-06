// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionManager"/>.
/// </summary>
[SingletonPerMicroserviceAndTenant]
public class ProjectionManager : IProjectionManager
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    readonly ILogger<ProjectionManager> _logger;
    readonly Dictionary<ProjectionId, IProjection> _projections = new();
    readonly Dictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionManager"/> class.
    /// </summary>
    /// <param name="executionContextManager">The <see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="projectionFactory"><see cref="IProjectionFactory"/> to use.</param>
    /// <param name="projectionPipelineFactory"><see cref="IProjectionPipelineFactory"/> to use.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ProjectionManager(
        IExecutionContextManager executionContextManager,
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory,
        ILogger<ProjectionManager> logger)
    {
        _executionContextManager = executionContextManager;
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool Exists(ProjectionId id) => _projections.ContainsKey(id);

    /// <inheritdoc/>
    public IProjection Get(ProjectionId id)
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
        using var scope = _logger.BeginProjectionManagerScope(_executionContextManager.Current.MicroserviceId, _executionContextManager.Current.TenantId);

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
