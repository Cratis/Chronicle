// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when the target view of a relation is not anchored at the node at the far end of the edge.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the relation member.</param>
/// <param name="expected">The label at the far end of the edge.</param>
/// <param name="actual">The label the target view is anchored at.</param>
public class TargetLabelMismatch(Type viewType, string member, string expected, string actual)
    : InvalidGraphView(viewType, nameof(TargetLabelMismatch), $"The relation '{member}' of the view '{viewType.FullName}' reaches the node '{expected}', but its target view is anchored at '{actual}'.");
