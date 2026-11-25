// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when two views in the same graph have the same name.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="name">The name the views share.</param>
/// <param name="graph">The name of the graph.</param>
public class DuplicateViewName(Type viewType, string name, string graph)
    : InvalidGraphView(viewType, nameof(DuplicateViewName), $"The view '{viewType.FullName}' is named '{name}', which another view in the graph '{graph}' is named too. View names are unique in a graph.");
