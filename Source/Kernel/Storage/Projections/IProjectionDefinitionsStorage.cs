// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections;

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
    /// Check if a <see cref="ProjectionDefinition"/> exists by its <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> to check for.</param>
    /// <returns>True if it exists, false if not.</returns>
    Task<bool> Has(ProjectionId id);

    /// <summary>
    /// Get a specific <see cref="ProjectionDefinition"/> by its <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> to get for.</param>
    /// <returns><see cref="ProjectionDefinition"/>.</returns>
    Task<ProjectionDefinition> Get(ProjectionId id);

    /// <summary>
    /// Delete a <see cref="ProjectionDefinition"/> by its <see cref="ProjectionId"/>.
    /// </summary>
    /// <param name="id"><see cref="ProjectionId"/> of the <see cref="ProjectionDefinition"/> to delete.</param>
    /// <returns>Awaitable task.</returns>
    Task Delete(ProjectionId id);

    /// <summary>
    /// Save a <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="definition">Definition to save.</param>
    /// <returns>Async task.</returns>
    Task Save(ProjectionDefinition definition);
}
