// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Graphs;

/// <summary>
/// Defines the clauses a related item must satisfy: equality on its property members.
/// </summary>
/// <typeparam name="TView">The type of the target view.</typeparam>
public interface IGraphRelatedClauses<TView>
    where TView : class
{
    /// <summary>
    /// Require a property member of the related item to equal a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="property">Selects a property member of the target view.</param>
    /// <param name="value">The value it must equal.</param>
    /// <returns>The clauses, for continuation.</returns>
    IGraphRelatedClauses<TView> Where<TValue>(Expression<Func<TView, TValue>> property, TValue value);
}
