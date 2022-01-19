// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Defines a system for working with <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    public interface IProjectionDefinitions
    {
        /// <summary>
        /// Get all projection definitions.
        /// </summary>
        /// <returns>Collection of <see cref="ProjectionDefinition"/>.</returns>
        IEnumerable<ProjectionDefinition> GetAll();

        /// <summary>
        /// Register <see cref="ProjectionDefinition"/> in the system.
        /// </summary>
        /// <param name="definition"><see cref="ProjectionDefinition"/> to register.</param>
        /// <returns>Async Task.</returns>
        Task Register(ProjectionDefinition definition);

        /// <summary>
        /// Check if a projection has a definition, based on its identifier.
        /// </summary>
        /// <param name="projectionId"><see cref="ProjectionId"/> to check for.</param>
        /// <returns>True if exists, false if not.</returns>
        Task<bool> HasFor(ProjectionId projectionId);

        /// <summary>
        /// Get the <see cref="ProjectionDefinition"/> based on its identifier.
        /// </summary>
        /// <param name="projectionId"><see cref="ProjectionId"/> to get for.</param>
        /// <returns><see cref="ProjectionDefinition"/> instance.</returns>
        Task<ProjectionDefinition> GetFor(ProjectionId projectionId);

        /// <summary>
        /// Compare an incoming <see cref="ProjectionDefinition"/> to existing to see if the definition has changed.
        /// </summary>
        /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> to compare.</param>
        /// <returns>True if it has changed, false if not.</returns>
        Task<bool> HasChanged(ProjectionDefinition projectionDefinition);
    }
}
