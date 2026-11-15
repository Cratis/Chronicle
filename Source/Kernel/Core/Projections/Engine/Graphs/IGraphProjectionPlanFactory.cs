// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Graphs;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Graphs;

namespace Cratis.Chronicle.Projections.Engine.Graphs;

/// <summary>
/// Defines a factory that resolves a <see cref="GraphProjectionDefinition"/> to a <see cref="GraphProjectionPlan"/>.
/// </summary>
public interface IGraphProjectionPlanFactory
{
    /// <summary>
    /// Create a plan.
    /// </summary>
    /// <param name="projection">The <see cref="ProjectionId"/> the plan is for.</param>
    /// <param name="definition">The <see cref="GraphProjectionDefinition"/> to resolve.</param>
    /// <param name="graph">The <see cref="GraphDefinition"/> of the graph the projection targets.</param>
    /// <returns>The <see cref="GraphProjectionPlan"/>.</returns>
    /// <exception cref="InvalidGraphProjection">Thrown when the definition does not fit the graph.</exception>
    GraphProjectionPlan Create(ProjectionId projection, GraphProjectionDefinition definition, GraphDefinition graph);
}
