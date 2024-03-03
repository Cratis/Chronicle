// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Kernel.Grains.Projections.Definitions;

/// <summary>
/// Defines a system for working with <see cref="ProjectionDefinition">projection definitions</see>.
/// </summary>
public interface IProjectionDefinitions
{
    /// <summary>
    /// Get all projection definitions.
    /// </summary>
    /// <returns>Collection of <see cref="ProjectionDefinition"/>.</returns>
    Task<IEnumerable<ProjectionDefinition>> GetAll();

    /// <summary>
    /// Register <see cref="ProjectionDefinition"/> in the system.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to register.</param>
    /// <returns>Async Task.</returns>
    Task Register(ProjectionDefinition definition);

    /// <summary>
    /// Try to get the <see cref="ProjectionDefinition"/> based on its identifier.
    /// </summary>
    /// <param name="projectionId"><see cref="ProjectionId"/> to get for.</param>
    /// <returns>Tuple of boolean telling if it was found and a <see cref="ProjectionDefinition"/> instance, if found.</returns>
    Task<(bool Found, ProjectionDefinition? Projection)> TryGetFor(ProjectionId projectionId);

    /// <summary>
    /// Compare an incoming <see cref="ProjectionDefinition"/> to existing to see if the definition has changed.
    /// </summary>
    /// <param name="projectionDefinition"><see cref="ProjectionDefinition"/> to compare.</param>
    /// <returns>Tuple containing a boolean for whether or not it is new and one for whether or not it has changed. Both can be false, meaning we the exact same projection definition.</returns>
    Task<(bool IsNew, bool HasChanged)> IsNewOrChanged(ProjectionDefinition projectionDefinition);
}
