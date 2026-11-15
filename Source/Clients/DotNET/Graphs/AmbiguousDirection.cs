// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a relation follows an edge between nodes of the same type without saying which way.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the relation member.</param>
/// <param name="edge">The edge type.</param>
public class AmbiguousDirection(Type viewType, string member, string edge)
    : InvalidGraphView(viewType, nameof(AmbiguousDirection), $"The relation '{member}' of the view '{viewType.FullName}' follows the edge '{edge}', whose source and target are the same node type. Give the relation an explicit direction.");
