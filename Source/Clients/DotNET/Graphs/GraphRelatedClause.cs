// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a clause that holds when the node has at least one edge along a relation whose far node satisfies every
/// nested clause. The nested clauses are property clauses on the target view.
/// </summary>
/// <param name="Member">The name of the relation member.</param>
/// <param name="Clauses">The nested <see cref="GraphPropertyClause">clauses</see>, all conjoined; empty means any related node.</param>
public sealed record GraphRelatedClause(string Member, IReadOnlyList<GraphPropertyClause> Clauses) : GraphQueryClause;
