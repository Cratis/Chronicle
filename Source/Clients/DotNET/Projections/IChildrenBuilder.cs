// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the base interface for children builders.
/// </summary>
public interface IChildrenBuilder
{
    /// <summary>
    /// Gets whether or not the builder has identified by.
    /// </summary>
    bool HasIdentifiedBy { get; }

    /// <summary>
    /// Gets the property path that identifies the child read model in the collection within the parent.
    /// </summary>
    /// <returns><see cref="PropertyPath"/> for the identified by.</returns>
    PropertyPath GetIdentifiedBy();
}

/// <summary>
/// Defines the builder for building out a child relationship on a model.
/// </summary>
/// <typeparam name="TParentReadModel">Parent read model type.</typeparam>
/// <typeparam name="TChildReadModel">Child read model type.</typeparam>
public interface IChildrenBuilder<TParentReadModel, TChildReadModel> : IProjectionBuilder<TChildReadModel, IChildrenBuilder<TParentReadModel, TChildReadModel>>, IChildrenBuilder
{
    /// <summary>
    /// Sets the property that identifies the child read model in the collection within the parent.
    /// </summary>
    /// <param name="propertyPath">The <see cref="PropertyPath"/>  that represents the property used to identify.</param>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy(PropertyPath propertyPath);

    /// <summary>
    /// Sets the property that identifies the child read model in the collection within the parent.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentReadModel, TChildReadModel> IdentifiedBy<TProperty>(Expression<Func<TChildReadModel, TProperty>> propertyExpression);

    /// <summary>
    /// Defines a child-specific join on the identity configured with IdentifiedBy().
    /// IdentifiedBy() must be called before Join().
    /// </summary>
    /// <typeparam name="TEvent">Type of event to join.</typeparam>
    /// <param name="builderCallback">Optional callback for configuring the child join.</param>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentReadModel, TChildReadModel> Join<TEvent>(Action<IChildrenJoinBuilder<TChildReadModel, TEvent>>? builderCallback = default);

    /// <summary>
    /// Defines the event and property on it that the child should be created as a value from.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property on the event to use.</param>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentReadModel, TChildReadModel> FromEventProperty<TEvent>(Expression<Func<TEvent, TChildReadModel>> propertyExpression);

    /// <summary>
    /// Insert new children first in the collection instead of appending them.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    /// <exception cref="ChildrenOrderingAlreadyDefined">Thrown when an ordering is already defined for the children.</exception>
    /// <remarks>
    /// Ordering is stored order, applied when a child is added. Updating an existing child never moves it.
    /// </remarks>
    /// <exception cref="ProjectionBuilderOperationNotSupported">Thrown when the builder implementation does not support the operation.</exception>
    IChildrenBuilder<TParentReadModel, TChildReadModel> Prepend() =>
        throw new ProjectionBuilderOperationNotSupported(GetType(), nameof(Prepend));

    /// <summary>
    /// Keep the collection sorted ascending by a property on the child when new children are added.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property to sort by.</typeparam>
    /// <param name="propertyExpression">The child property to sort by.</param>
    /// <returns>Builder continuation.</returns>
    /// <exception cref="ChildrenOrderingAlreadyDefined">Thrown when an ordering is already defined for the children.</exception>
    /// <remarks>
    /// Ordering is stored order, applied when a child is added. Children without a value sort first. Updating an
    /// existing child never moves it, even when the property it is sorted by changes.
    /// </remarks>
    /// <exception cref="ProjectionBuilderOperationNotSupported">Thrown when the builder implementation does not support the operation.</exception>
    IChildrenBuilder<TParentReadModel, TChildReadModel> OrderBy<TProperty>(Expression<Func<TChildReadModel, TProperty>> propertyExpression) =>
        throw new ProjectionBuilderOperationNotSupported(GetType(), nameof(OrderBy));

    /// <summary>
    /// Keep the collection sorted descending by a property on the child when new children are added.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property to sort by.</typeparam>
    /// <param name="propertyExpression">The child property to sort by.</param>
    /// <returns>Builder continuation.</returns>
    /// <exception cref="ChildrenOrderingAlreadyDefined">Thrown when an ordering is already defined for the children.</exception>
    /// <remarks>
    /// Ordering is stored order, applied when a child is added. Children without a value sort last. Updating an
    /// existing child never moves it, even when the property it is sorted by changes.
    /// </remarks>
    /// <exception cref="ProjectionBuilderOperationNotSupported">Thrown when the builder implementation does not support the operation.</exception>
    IChildrenBuilder<TParentReadModel, TChildReadModel> OrderByDescending<TProperty>(Expression<Func<TChildReadModel, TProperty>> propertyExpression) =>
        throw new ProjectionBuilderOperationNotSupported(GetType(), nameof(OrderByDescending));
}
