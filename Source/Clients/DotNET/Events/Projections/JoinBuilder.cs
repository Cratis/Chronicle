// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of the <see cref="IJoinBuilder{TModel, TEvent}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public class JoinBuilder<TModel, TEvent> : FromBuilder<TModel, TEvent>, IJoinBuilder<TModel, TEvent>
{
    /// <inheritdoc/>
    public IJoinBuilder<TModel, TEvent> On<TProperty>(Expression<Func<TModel, TProperty>> keyAccessor) => throw new NotImplementedException();
}
