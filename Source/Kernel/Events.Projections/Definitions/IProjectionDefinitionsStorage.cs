// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Projections.Definitions
{
    /// <summary>
    /// Defines a system for working with <see cref="ProjectionDefinition"/>.
    /// </summary>
    public interface IProjectionDefinitionsStorage
    {
        /// <summary>
        /// Get all <see cref="ProjectionDefinition">definitions</see> registered.
        /// </summary>
        /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
        Task<IEnumerable<ProjectionDefinition>> GetAll();

        /// <summary>
        /// Save a <see cref="ProjectionDefinition"/>.
        /// </summary>
        /// <param name="definition">Definition to save.</param>
        /// <returns>Async task.</returns>
        Task Save(ProjectionDefinition definition);
    }
}
