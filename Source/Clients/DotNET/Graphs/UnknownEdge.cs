// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a relation of a view names an edge type the graph does not declare.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the relation member.</param>
/// <param name="edge">The edge type it names.</param>
public class UnknownEdge(Type viewType, string member, string edge)
    : InvalidGraphView(viewType, nameof(UnknownEdge), $"The relation '{member}' of the view '{viewType.FullName}' follows the edge '{edge}', which its graph does not declare.");
