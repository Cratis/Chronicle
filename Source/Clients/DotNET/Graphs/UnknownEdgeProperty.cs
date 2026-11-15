// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a member of a view names a property the edge that was followed does not declare.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the member.</param>
/// <param name="property">The edge property it names.</param>
/// <param name="edge">The edge type.</param>
public class UnknownEdgeProperty(Type viewType, string member, string property, string edge)
    : InvalidGraphView(viewType, nameof(UnknownEdgeProperty), $"The member '{member}' of the view '{viewType.FullName}' reads the property '{property}', which the edge '{edge}' does not declare.");
