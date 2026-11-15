// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a query over a view.
/// </summary>
/// <param name="View">The name of the view to read.</param>
/// <param name="Clauses">The clauses that select the roots, all of which must hold.</param>
/// <param name="FirstOnly">Whether to give only the first root, in key order.</param>
public record GraphViewQuery(string View, IReadOnlyList<GraphViewClause> Clauses, bool FirstOnly = false);
