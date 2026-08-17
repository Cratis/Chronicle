// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Orleans.Concurrency;

namespace Cratis.Chronicle.Projections;

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
    [AlwaysInterleave]
    Task<IEnumerable<ProjectionDefinition>> GetProjectionDefinitions();

    /// <summary>
    /// Get all the projections with their declaration representation.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionWithDeclaration"/>.</returns>
    Task<IEnumerable<ProjectionWithDeclaration>> GetProjectionDeclarations();

    /// <summary>
    /// Register a set of <see cref="ProjectionDefinition"/> for the event store it belongs to.
    /// </summary>
    /// <param name="definitions">A collection of <see cref="ProjectionDefinition"/>.</param>
    /// <param name="fullSetOwner">
    /// When set, the registration is the complete set of projections for that <see cref="ProjectionOwner"/> and any
    /// registered projection with the same owner that is not in the set is retired: its observer is unsubscribed in
    /// every namespace, its jobs and failed partitions are cleared, its definition is removed from the engine and from
    /// storage, and its sink container is left untouched. Leave unset for a partial registration (for example saving a
    /// single projection), which must never retire anything.
    /// </param>
    /// <returns>Awaitable task.</returns>
    Task Register(IEnumerable<ProjectionDefinition> definitions, ProjectionOwner? fullSetOwner = null);
}
