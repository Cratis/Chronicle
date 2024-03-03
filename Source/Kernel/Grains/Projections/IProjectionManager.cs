// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Projections.Pipelines;
using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;
using EngineProjection = Aksio.Cratis.Kernel.Projections.IProjection;

namespace Aksio.Cratis.Kernel.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Check whether or not a projection exists.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> to check.</param>
    /// <returns>True if it exists, false if not.</returns>
    bool Exists(ProjectionId id);

    /// <summary>
    /// Get a projection by its <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="Projection"/> instance.</param>
    /// <returns>A projection instance.</returns>
    EngineProjection Get(ProjectionId id);

    /// <summary>
    /// Get a projection pipeline by its <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> to get for.</param>
    /// <returns>An instance of the <see cref="IProjectionPipeline"/>.</returns>
    IProjectionPipeline GetPipeline(ProjectionId id);

    /// <summary>
    /// Register a projection.
    /// </summary>
    /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> to register.</param>
    /// <param name="pipelineDefinition"><see cref="ProjectionPipelineDefinition"/> to register.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(ProjectionDefinition projectionDefinition, ProjectionPipelineDefinition pipelineDefinition);
}
