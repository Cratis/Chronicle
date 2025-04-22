// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building properties on a model.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public interface IModelPropertiesBuilder<TModel, TEvent, TBuilder>
    where TBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TBuilder>
{
    /// <summary>
    /// Automatically map event properties to model properties on the events added.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    TBuilder AutoMap();

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
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property within <see cref="EventContext"/> to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor);

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
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingParentKeyFromContext<TProperty>(Expression<Func<EventContext, TProperty>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a composite of expressions.
    /// </summary>
    /// <typeparam name="TKeyType">Type of key.</typeparam>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);

    /// <summary>
    /// Increment the value of a property on the model.
    /// </summary>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <returns>Builder continuation.</returns>
    TBuilder Increment<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Increment the value of a property on the model.
    /// </summary>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <returns>Builder continuation.</returns>
    TBuilder Decrement<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Start building the add operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    IAddBuilder<TModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Start building the add operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    ISubtractBuilder<TModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Start building the count operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder Count<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Start building a child operation that add a child based on a property on the event.
    /// </summary>
    /// <param name="targetProperty">The collection property that will receive the child.</param>
    /// <param name="eventProperty">Event property accessor for defining the source property.</param>
    /// <typeparam name="TChildModel">Type of child model.</typeparam>
    /// <returns>Builder continuation.</returns>
    TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<TEvent, TChildModel>> eventProperty);

    /// <summary>
    /// Start building the add child operation to a target property holding an collection of a specific child model type.
    /// </summary>
    /// <param name="targetProperty">The collection property that will receive the child.</param>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    /// <typeparam name="TChildModel">Type of child model.</typeparam>
    /// <returns>Builder continuation.</returns>
    TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, TEvent>> builderCallback);

    /// <summary>
    /// Start building the set operation to a target property on the model.
    /// </summary>
    /// <param name="propertyPath">Model property path for defining the target property.</param>
    /// <returns>The <see cref="ISetBuilder{TModel, TEvent, TBuilder}"/> to continue building on.</returns>
    ISetBuilder<TModel, TEvent, TBuilder> Set(PropertyPath propertyPath);

    /// <summary>
    /// Start building the set operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>The <see cref="ISetBuilder{TModel, TEvent, TProperty, TBuilder}"/> to continue building on.</returns>
    ISetBuilder<TModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Start building the set operation to target the value instance itself.
    /// </summary>
    /// <returns>The <see cref="ISetBuilder{TModel, TEvent, TBuilder}"/> to continue building on.</returns>
    ISetBuilder<TModel, TEvent, TBuilder> SetThisValue();
}
