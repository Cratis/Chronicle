// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents a backend-neutral query over a view: the AST an <see cref="IGraphQueryExecutor"/> translates and runs.
/// </summary>
/// <param name="Graph">The name of the graph.</param>
/// <param name="Shape">The <see cref="GraphQueryShape"/>.</param>
/// <param name="Root">The <see cref="GraphQueryNode"/> of the view being queried.</param>
/// <param name="Clauses">The <see cref="GraphQueryClause">clauses</see> on the root, all conjoined.</param>
public sealed record GraphQuery(string Graph, GraphQueryShape Shape, GraphQueryNode Root, IReadOnlyList<GraphQueryClause> Clauses);
