// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphQuery{TView}"/>.
/// </summary>
/// <typeparam name="TView">The type of the view.</typeparam>
/// <param name="views">The <see cref="IGraphViews"/> the view belongs to.</param>
/// <param name="executor">The <see cref="IGraphQueryExecutor"/> that runs the query.</param>
/// <param name="hydrator">The <see cref="GraphRowHydrator"/> that builds the views.</param>
internal sealed class GraphQueryBuilder<TView>(IGraphViews views, IGraphQueryExecutor executor, GraphRowHydrator hydrator) : IGraphQuery<TView>
    where TView : class
{
    readonly string _graph = views.GraphOf(typeof(TView));
    readonly GraphQueryNode _node = views.NodeOf(typeof(TView));
    readonly GraphViewMembers _members = new(views);
    readonly List<GraphQueryClause> _clauses = [];

    /// <inheritdoc/>
    public IGraphQuery<TView> Where<TValue>(Expression<Func<TView, TValue>> property, TValue value)
    {
        _clauses.Add(_members.Property(typeof(TView), _node, property, value));
        return this;
    }

    /// <inheritdoc/>
    public IGraphQuery<TView> WhereRelated(Expression<Func<TView, object?>> relation)
    {
        _clauses.Add(new GraphRelatedClause(RelationOf(relation).Name, []));
        return this;
    }

    /// <inheritdoc/>
    public IGraphQuery<TView> WhereRelated<TRelated>(Expression<Func<TView, object?>> relation, Action<IGraphRelatedClauses<TRelated>> clauses)
        where TRelated : class
    {
        var member = RelationOf(relation);
        if (!views.IsView(typeof(TRelated)) || !ReferenceEquals(views.NodeOf(typeof(TRelated)), member.Relation!.Target))
        {
            throw new RelatedViewTypeMismatch(typeof(TView), member.Name, typeof(TRelated));
        }

        var related = new GraphRelatedClauses<TRelated>(_members, member.Relation.Target);
        clauses(related);
        _clauses.Add(new GraphRelatedClause(member.Name, related.Clauses));
        return this;
    }

    /// <inheritdoc/>
    public GraphQuery ToQuery(GraphQueryShape shape) => new(_graph, shape, _node, [.. _clauses]);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TView>> ToList(CancellationToken cancellationToken = default)
    {
        var rows = await executor.Execute(ToQuery(GraphQueryShape.List), cancellationToken);
        return [.. rows.Select(row => hydrator.Hydrate<TView>(row)!)];
    }

    /// <inheritdoc/>
    public async Task<TView?> FirstOrDefault(CancellationToken cancellationToken = default)
    {
        var rows = await executor.Execute(ToQuery(GraphQueryShape.First), cancellationToken);
        return rows.Count == 0 ? null : hydrator.Hydrate<TView>(rows[0]);
    }

    GraphQueryMember RelationOf(LambdaExpression relation)
    {
        var member = _members.Resolve(typeof(TView), _node, relation);
        return member.Relation is null ? throw new GraphViewMemberIsNotARelation(typeof(TView), member.Name) : member;
    }
}
