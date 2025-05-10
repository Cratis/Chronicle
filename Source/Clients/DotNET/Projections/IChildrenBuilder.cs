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
    /// Gets the property path that identifies the child model in the collection within the parent.
    /// </summary>
    /// <returns><see cref="PropertyPath"/> for the identified by.</returns>
    PropertyPath GetIdentifiedBy();
}

/// <summary>
/// Defines the builder for building out a child relationship on a model.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
public interface IChildrenBuilder<TParentModel, TChildModel> : IProjectionBuilder<TChildModel, IChildrenBuilder<TParentModel, TChildModel>>, IChildrenBuilder
{
    /// <summary>
    /// Sets the property that identifies the child model in the collection within the parent.
    /// </summary>
    /// <param name="propertyPath">The <see cref="PropertyPath"/>  that represents the property used to identify.</param>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy(PropertyPath propertyPath);

    /// <summary>
    /// Sets the property that identifies the child model in the collection within the parent.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression);

    /// <summary>
    /// Defines the event and property on it that the child should be created as a value from.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property on the event to use.</param>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentModel, TChildModel> FromEventProperty<TEvent>(Expression<Func<TEvent, TChildModel>> propertyExpression);
}
