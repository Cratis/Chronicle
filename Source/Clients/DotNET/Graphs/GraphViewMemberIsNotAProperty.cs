// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when an equality clause names a member that is not a property of the node.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The member.</param>
public class GraphViewMemberIsNotAProperty(Type viewType, string member)
    : Exception($"The member '{member}' of the view '{viewType.FullName}' is not a property of its node, so it cannot be compared.");
