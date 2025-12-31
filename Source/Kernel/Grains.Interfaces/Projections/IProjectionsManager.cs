// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Defines a system that is responsible for supervises projections in the system.
/// </summary>
public interface IProjectionsManager : IGrainWithStringKey
{
    /// <summary>
    /// Ensure the existence of the projections manager.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Ensure();

    /// <summary>
    /// Get all the <see cref="ProjectionDefinition">projection definitions</see> available.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions();

    /// <summary>
    /// Get all the projections with their DSL representation.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionWithDsl"/>.</returns>
    Task<IEnumerable<ProjectionWithDsl>> GetProjectionDsls();

    /// <summary>
    /// Register a set of <see cref="ProjectionDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definitions">A collection of <see cref="ProjectionDefinition"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<ProjectionDefinition> definitions);
}
