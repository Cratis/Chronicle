// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Defines a factory that creates the engine projection for a projection that targets a graph.
/// </summary>
public interface IGraphProjectionFactory
{
    /// <summary>
    /// Create the <see cref="IProjection"/>.
    /// </summary>
    /// <param name="definition">The <see cref="ProjectionDefinition"/>, which has graph mappings.</param>
    /// <param name="readModel">The <see cref="ReadModelDefinition"/> whose schema describes the graph.</param>
    /// <param name="fingerprint">The definition fingerprint of the projection.</param>
    /// <returns>The <see cref="IProjection"/>.</returns>
    /// <exception cref="InvalidGraphProjection">Thrown when the definition does not fit the graph.</exception>
    IProjection Create(ProjectionDefinition definition, ReadModelDefinition readModel, string fingerprint);
}
