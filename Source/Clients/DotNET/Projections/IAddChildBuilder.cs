// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a builder for building an add child operation, typically used in to a from expression.
/// </summary>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <typeparam name="TEvent">Type of event.</typeparam>
public interface IAddChildBuilder<TChildModel, TEvent>
{
    /// <summary>
    /// Define what key to use. This is optional, if not set - it will default to using the event source identifier on the event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a value in the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="keyAccessor">Accessor for the property within <see cref="EventContext"/> to use.</param>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor);

    /// <summary>
    /// Define what property on the event represents the parent key. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Define what composite key based on properties on the event represents the parent key. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <typeparam name="TKeyType">Type of key.</typeparam>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);

    /// <summary>
    /// Define what property on the event represents the parent key based on a property in the <see cref="EventContext"/>. This is typically used in child relationships to identify the parent model to
    /// work with.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> UsingParentKeyFromContext<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Sets the property that identifies the child model in the collection within the parent.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Builder continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression);

    /// <summary>
    /// Describe an adding of a child from an object already on the event.
    /// </summary>
    /// <param name="propertyWithChild">The property that holds object on the event.</param>
    /// <returns>Parent builder for continuation.</returns>
    IAddChildBuilder<TChildModel, TEvent> FromObject(Expression<Func<TEvent, TChildModel>> propertyWithChild);
}
