// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Projections.Definitions;

namespace Cratis.Events.Projections
{
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
        IProjection CreateFrom(ProjectionDefinition definition);
    }
}
