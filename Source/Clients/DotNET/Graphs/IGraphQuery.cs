// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines a query for the roots of a view. Clauses are conjoined and only select roots; a relation always holds every
/// related item.
/// </summary>
/// <typeparam name="TView">The type of the view.</typeparam>
public interface IGraphQuery<TView>
    where TView : class
{
    /// <summary>
    /// Keep the roots whose property member equals a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="property">Selects a property member of the view.</param>
    /// <param name="value">The value it must equal.</param>
    /// <returns>The query, for continuation.</returns>
    IGraphQuery<TView> Where<TValue>(Expression<Func<TView, TValue>> property, TValue value);

    /// <summary>
    /// Keep the roots that have at least one item along a relation.
    /// </summary>
    /// <param name="relation">Selects a relation member of the view.</param>
    /// <returns>The query, for continuation.</returns>
    IGraphQuery<TView> WhereRelated(Expression<Func<TView, object?>> relation);

    /// <summary>
    /// Keep the roots that have at least one item along a relation that satisfies every nested clause.
    /// </summary>
    /// <typeparam name="TRelated">The type of the target view of the relation.</typeparam>
    /// <param name="relation">Selects a relation member of the view.</param>
    /// <param name="clauses">Adds the clauses on the related item.</param>
    /// <returns>The query, for continuation.</returns>
    IGraphQuery<TView> WhereRelated<TRelated>(Expression<Func<TView, object?>> relation, Action<IGraphRelatedClauses<TRelated>> clauses)
        where TRelated : class;

    /// <summary>
    /// Get the backend-neutral query this builder describes.
    /// </summary>
    /// <param name="shape">The <see cref="GraphQueryShape"/>.</param>
    /// <returns>The <see cref="GraphQuery"/>.</returns>
    GraphQuery ToQuery(GraphQueryShape shape);

    /// <summary>
    /// Run the query for every matching root, ordered by key.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the read.</param>
    /// <returns>The views.</returns>
    Task<IReadOnlyList<TView>> ToList(CancellationToken cancellationToken = default);

    /// <summary>
    /// Run the query for the first matching root in key order.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to cancel the read.</param>
    /// <returns>The view, or null when no root matches.</returns>
    Task<TView?> FirstOrDefault(CancellationToken cancellationToken = default);
}
