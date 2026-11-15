// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.Graphs.Views;

/// <summary>
/// Represents a clause that keeps the roots that have at least one related node satisfying the nested clauses.
/// </summary>
/// <param name="Member">The name of a relation member of the view.</param>
/// <param name="Clauses">The property clauses the related node must satisfy, on the members of the related view.</param>
public sealed record GraphViewRelatedClause(string Member, IReadOnlyList<GraphViewPropertyClause> Clauses) : GraphViewClause;
