// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a view reads edge properties and relations of more than one edge type lead to it.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="edges">The edge types of the relations that lead to it.</param>
public class EdgePropertyViewSharedAcrossEdges(Type viewType, IEnumerable<string> edges)
    : InvalidGraphView(viewType, nameof(EdgePropertyViewSharedAcrossEdges), $"The view '{viewType.FullName}' reads properties of an edge, but relations along different edges lead to it: {string.Join(", ", edges)}. Declare a view per edge.");
