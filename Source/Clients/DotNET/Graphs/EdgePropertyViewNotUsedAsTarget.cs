// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Exception that gets thrown when a view reads edge properties but no relation leads to it.
/// </summary>
/// <param name="viewType">The type of the view.</param>
public class EdgePropertyViewNotUsedAsTarget(Type viewType)
    : InvalidGraphView(viewType, nameof(EdgePropertyViewNotUsedAsTarget), $"The view '{viewType.FullName}' reads properties of an edge, but no relation leads to it, so there is no edge to read them from.");
