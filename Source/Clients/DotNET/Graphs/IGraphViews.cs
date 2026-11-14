// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Serialization;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the views an application declares over its property graphs, after they have been checked.
/// </summary>
public interface IGraphViews
{
    /// <summary>
    /// Gets the definitions of the valid views, one per graph that has views, ordered by graph name.
    /// </summary>
    IReadOnlyList<GraphViewDefinition> Definitions { get; }

    /// <summary>
    /// Gets the views that were left out, with the <see cref="InvalidGraphView"/> that says why.
    /// </summary>
    IReadOnlyDictionary<Type, Exception> Failures { get; }

    /// <summary>
    /// Gets the <see cref="INamingPolicy"/> member and property names were given with.
    /// </summary>
    INamingPolicy NamingPolicy { get; }

    /// <summary>
    /// Check whether a type is a valid view.
    /// </summary>
    /// <param name="viewType">The type to check.</param>
    /// <returns>True if it is a valid view, false if not.</returns>
    bool IsView(Type viewType);

    /// <summary>
    /// Get the name of the graph a view reads.
    /// </summary>
    /// <param name="viewType">The type of the view.</param>
    /// <returns>The name of the graph.</returns>
    /// <exception cref="NotAGraphView">Thrown when the type is not a valid view.</exception>
    string GraphOf(Type viewType);

    /// <summary>
    /// Get the resolved query node of a view: its root label, key and members, with every relation resolved to the
    /// node of its target view.
    /// </summary>
    /// <param name="viewType">The type of the view.</param>
    /// <returns>The <see cref="GraphQueryNode"/>.</returns>
    /// <exception cref="NotAGraphView">Thrown when the type is not a valid view.</exception>
    GraphQueryNode NodeOf(Type viewType);

    /// <summary>
    /// Get the resolved query node of a view by its name.
    /// </summary>
    /// <param name="graph">The name of the graph.</param>
    /// <param name="view">The name of the view.</param>
    /// <returns>The <see cref="GraphQueryNode"/>.</returns>
    /// <exception cref="UnknownGraphView">Thrown when the graph has no valid view with the name.</exception>
    GraphQueryNode NodeOf(string graph, string view);
}
