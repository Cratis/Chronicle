// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a relation follows an edge that does not start or end at the root of the view.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the relation member.</param>
/// <param name="edge">The edge type.</param>
/// <param name="label">The label of the root node.</param>
public class EdgeNotIncidentOnRoot(Type viewType, string member, string edge, string label)
    : InvalidGraphView(viewType, nameof(EdgeNotIncidentOnRoot), $"The relation '{member}' of the view '{viewType.FullName}' follows the edge '{edge}', which does not start or end at the node '{label}' in the direction it is followed.");
