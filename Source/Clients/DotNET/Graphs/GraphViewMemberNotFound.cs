// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a query clause names a member the view does not have.
/// </summary>
/// <param name="viewType">The type of the view.</param>
/// <param name="member">The member.</param>
public class GraphViewMemberNotFound(Type viewType, string member)
    : Exception($"The view '{viewType.FullName}' has no member '{member}'.");
