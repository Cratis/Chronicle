// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a member of a view names a property the root node does not declare.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The name of the member.</param>
/// <param name="property">The node property it names.</param>
/// <param name="label">The label of the root node.</param>
public class UnknownNodeProperty(Type viewType, string member, string property, string label)
    : InvalidGraphView(viewType, nameof(UnknownNodeProperty), $"The member '{member}' of the view '{viewType.FullName}' reads the property '{property}', which the node '{label}' does not declare.");
