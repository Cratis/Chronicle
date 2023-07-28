using System.Collections.Concurrent;
// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Engines.Projections.Pipelines;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionManager"/>.
/// </summary>
[SingletonPerMicroservice]
public class ProjectionManager : IProjectionManager
{
    readonly IProjectionFactory _projectionFactory;
    readonly IProjectionPipelineFactory _projectionPipelineFactory;
    ConcurrentDictionary<ProjectionId, IProjection> _projections = new();
    ConcurrentDictionary<ProjectionId, IProjectionPipeline> _pipelines = new();

    public ProjectionManager(
        IProjectionFactory projectionFactory,
        IProjectionPipelineFactory projectionPipelineFactory)
    {
        _projectionFactory = projectionFactory;
        _projectionPipelineFactory = projectionPipelineFactory;
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
