// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections.Pipelines
{
    /// <summary>
    /// Defines a system for working with <see cref="IProjectionPipeline">projection pipelines</see>.
    /// </summary>
    public interface IProjectionPipelineFactory
    {
        /// <summary>
        /// Get a projection pipeline based on <see cref="ProjectionPipelineDefinition"/>.
        /// </summary>
        /// <param name="projection"><see cref="IProjection"/> the pipeline is for.</param>
        /// <param name="definition">The <see cref="ProjectionPipelineDefinition"/> to register.</param>
        /// <returns>The <see cref="IProjectionPipeline"/> instance.</returns>
        IProjectionPipeline CreateFrom(IProjection projection, ProjectionPipelineDefinition definition);
    }
}
