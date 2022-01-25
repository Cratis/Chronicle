// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Defines a system for working with <see cref="ProjectionDefinition"/>.
    /// </summary>
    public interface IProjectionPipelineDefinitionsStorage
    {
        /// <summary>
        /// Get all <see cref="ProjectionPipelineDefinition">definitions</see> registered.
        /// </summary>
        /// <returns>A collection of <see cref="ProjectionPipelineDefinition"/>.</returns>
        Task<IEnumerable<ProjectionPipelineDefinition>> GetAll();

        /// <summary>
        /// Save a <see cref="ProjectionPipelineDefinition"/>.
        /// </summary>
        /// <param name="definition">Definition to save.</param>
        /// <returns>Async task.</returns>
        Task Save(ProjectionPipelineDefinition definition);
    }
}
