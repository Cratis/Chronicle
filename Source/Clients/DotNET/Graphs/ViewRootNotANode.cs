// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when the root of the view is not a node declared in the graph.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="rootType">The type the view names as its root.</param>
public class ViewRootNotANode(Type viewType, Type rootType)
    : InvalidGraphView(viewType, nameof(ViewRootNotANode), $"The view '{viewType.FullName}' is anchored at '{rootType.FullName}', which is not a node declared in a graph.");
