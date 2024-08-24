// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IRemovedWithBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public class RemovedWithBuilder<TModel, TEvent, TBuilder>(IProjectionBuilder<TModel, TBuilder> projectionBuilder) : IRemovedWithBuilder<TModel, TEvent, TBuilder>
    where TBuilder : class
{
    /// <inheritdoc/>
    public TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingParentKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public RemovedWithDefinition Build() => throw new NotImplementedException();
}
