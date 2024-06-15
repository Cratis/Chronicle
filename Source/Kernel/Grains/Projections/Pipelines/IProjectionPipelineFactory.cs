// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.Pipelines;
using Cratis.Projections.Definitions;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Grains.Projections.Pipelines;

/// <summary>
/// Defines a system for working with <see cref="IProjectionPipeline">projection pipelines</see>.
/// </summary>
public interface IProjectionPipelineFactory
{
    /// <summary>
    /// Get a projection pipeline based on <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    /// <param name="projection"><see cref="EngineProjection"/> the pipeline is for.</param>
    /// <param name="definition">The <see cref="ProjectionPipelineDefinition"/> to register.</param>
    /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
    IProjectionPipeline CreateFrom(EngineProjection projection, ProjectionPipelineDefinition definition);
}
