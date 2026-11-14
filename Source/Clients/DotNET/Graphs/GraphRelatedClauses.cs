// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphRelatedClauses{TView}"/>.
/// </summary>
/// <typeparam name="TView">The type of the target view.</typeparam>
/// <param name="members">The <see cref="GraphViewMembers"/> for resolving members.</param>
/// <param name="node">The <see cref="GraphQueryNode"/> of the target view.</param>
internal sealed class GraphRelatedClauses<TView>(GraphViewMembers members, GraphQueryNode node) : IGraphRelatedClauses<TView>
    where TView : class
{
    readonly List<GraphPropertyClause> _clauses = [];

    /// <summary>
    /// Gets the clauses added.
    /// </summary>
    public IReadOnlyList<GraphPropertyClause> Clauses => _clauses;

    /// <inheritdoc/>
    public IGraphRelatedClauses<TView> Where<TValue>(Expression<Func<TView, TValue>> property, TValue value)
    {
        _clauses.Add(members.Property(typeof(TView), node, property, value));
        return this;
    }
}
