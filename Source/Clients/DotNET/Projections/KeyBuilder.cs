// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a default implementation of <see cref="IKeyBuilder{TEvent, TBuilder}"/> that works with <see cref="IEventValueExpression"/>.
/// </summary>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public class KeyBuilder<TEvent, TBuilder> : IKeyBuilder<TEvent, TBuilder>
    where TBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
    protected PropertyExpression _keyExpression = new EventSourceIdExpression().Build();
    protected PropertyExpression _parentKeyExpression = new NoExpression().Build();
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields

    /// <inheritdoc/>
    public TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _keyExpression = new EventContentPropertyExpression(keyAccessor.GetPropertyPath()).Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _keyExpression = new EventContentPropertyExpression(keyAccessor.GetPropertyPath()).Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback)
    {
        var compositeKeyBuilder = new CompositeKeyBuilder<TKeyType, TEvent>();
        builderCallback(compositeKeyBuilder);
        _parentKeyExpression = compositeKeyBuilder.Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        _parentKeyExpression = new EventContextPropertyExpression(keyAccessor.GetPropertyPath()).Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        _parentKeyExpression = new EventContextPropertyExpression(keyAccessor.GetPropertyPath()).Build();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback)
    {
        var compositeKeyBuilder = new CompositeKeyBuilder<TKeyType, TEvent>();
        builderCallback(compositeKeyBuilder);
        _keyExpression = compositeKeyBuilder.Build();
        return (this as TBuilder)!;
    }
}
