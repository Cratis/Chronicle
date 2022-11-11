// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines a builder for building an add child operation, typically used in to a from expression.
/// </summary>
/// <typeparam name="TChildModel">Child model type.</typeparam>
/// <typeparam name="TEvent">Type of event.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public interface IAddChildBuilder<TChildModel, TEvent, TParentBuilder>
{
    /// <summary>
    /// Sets the property that identifies the child model in the collection within the parent.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Builder continuation.</returns>
    TParentBuilder IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression);

    /// <summary>
    /// Describe an adding of a child from an object already on the event.
    /// </summary>
    /// <param name="propertyWithChild">The property that holds object on the event.</param>
    /// <returns>Parent builder for continuation.</returns>
    TParentBuilder FromObject(Expression<Func<TEvent, IEnumerable<TChildModel>>> propertyWithChild);
}
