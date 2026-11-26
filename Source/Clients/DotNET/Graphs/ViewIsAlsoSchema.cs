// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a view type is also a node or an edge.
/// </summary>
/// <param name="viewType">The type of the view.</param>
public class ViewIsAlsoSchema(Type viewType)
    : InvalidGraphView(viewType, nameof(ViewIsAlsoSchema), $"The type '{viewType.FullName}' is a view and also a node or an edge. A view only reads the graph; declare it as a separate type.");
