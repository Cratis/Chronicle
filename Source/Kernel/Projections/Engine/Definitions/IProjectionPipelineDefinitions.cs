// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Defines a system for working with <see cref="ProjectionPipelineDefinition"/>.
    /// </summary>
    public interface IProjectionPipelineDefinitions
    {
        /// <summary>
        /// Get all projection pipeline definitions.
        /// </summary>
        /// <returns>Collection of <see cref="ProjectionDefinition"/>.</returns>
        Task<IEnumerable<ProjectionPipelineDefinition>> GetAll();

        /// <summary>
        /// Register <see cref="ProjectionPipelineDefinition"/> in the system.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionPipelineDefinition"/> to register.</param>
        /// <returns>Async Task.</returns>
        Task Register(ProjectionPipelineDefinition definition);

        /// <summary>
        /// Check if a projection has a definition, based on its identifier.
        /// </summary>
        /// <param name="projectionId"><see cref="ProjectionId"/> to check for.</param>
        /// <returns>True if exists, false if not.</returns>
        Task<bool> HasFor(ProjectionId projectionId);

        /// <summary>
        /// Get the <see cref="ProjectionPipelineDefinition"/> based on its identifier.
        /// </summary>
        /// <param name="projectionId"><see cref="ProjectionId"/> to get for.</param>
        /// <returns><see cref="ProjectionPipelineDefinition"/> instance.</returns>
        Task<ProjectionPipelineDefinition> GetFor(ProjectionId projectionId);
    }
}
