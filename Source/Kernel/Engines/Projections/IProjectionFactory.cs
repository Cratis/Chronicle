// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Shared.Projections.Definitions;

namespace Aksio.Cratis.Kernel.Engines.Projections;

/// <summary>
/// Defines a factory for creating <see cref="IProjection"/>.
/// </summary>
public interface IProjectionFactory
{
    /// <summary>
    /// Create a <see cref="IProjection"/> from a <see cref="ProjectionDefinition"/>.
    /// </summary>
    /// <param name="definition"><see cref="ProjectionDefinition"/> to create from.</param>
    /// <returns>A new <see cref="IProjection"/> instance.</returns>
    Task<IProjection> CreateFrom(ProjectionDefinition definition);
}
