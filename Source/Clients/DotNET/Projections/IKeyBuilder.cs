// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a builder for keys.
/// </summary>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public interface IKeyBuilder<TEvent, TBuilder>
{
    /// <summary>
    /// Define what key to use. This is optional, if not set - it will default to using the event source identifier on the event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a value in the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="keyAccessor">Accessor for the property within <see cref="EventContext"/> to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor);

    /// <summary>
    /// Define what property on the event represents the parent key. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Define what composite key based on properties on the event represents the parent key. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <typeparam name="TKeyType">Type of key.</typeparam>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);

    /// <summary>
    /// Define what property on the event represents the parent key based on a property in the <see cref="EventContext"/>. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingParentKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a composite of expressions.
    /// </summary>
    /// <typeparam name="TKeyType">Type of key.</typeparam>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);
}
