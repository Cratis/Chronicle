// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Defines a projection of events onto a property graph, declared with a fluent builder.
/// </summary>
/// <remarks>
/// Give the class the graph attribute to name the graph. Without it the graph is named after the assembly's root namespace.
/// </remarks>
public interface IGraphProjection
{
    /// <summary>
    /// Define the nodes and edges of the graph and the events they are projected from.
    /// </summary>
    /// <param name="builder">The <see cref="IGraphProjectionBuilder"/> to define with.</param>
    void Define(IGraphProjectionBuilder builder);
}
