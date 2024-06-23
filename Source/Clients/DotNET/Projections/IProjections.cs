// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that holds all <see cref="ProjectionDefinition">projection definitions</see> defined through <see cref="IProjectionBuilderFor{T}"/>.
/// </summary>
public interface IProjections
{
    /// <summary>
    /// Gets all the <see cref="ProjectionDefinition">projection definitions</see>.
    /// </summary>
    IImmutableList<ProjectionDefinition> Definitions { get; }

    /// <summary>
    /// Discover all projections from entry assembly and dependencies.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Discover();

    /// <summary>
    /// Register all projections with Chronicle.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Register();
}
