// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Projections.Definitions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines the builder for building out a child relationship on a model.
/// </summary>
/// <typeparam name="TParentModel">Parent model type.</typeparam>
/// <typeparam name="TChildModel">Child model type.</typeparam>
public interface IChildrenBuilder<TParentModel, TChildModel> : IProjectionBuilder<TChildModel, IChildrenBuilder<TParentModel, TChildModel>>
{
    /// <summary>
    /// Sets the property that identifies the child model in the collection within the parent.
    /// </summary>
    /// <param name="propertyExpression">The expression that represents the property used to identify.</param>
    /// <typeparam name="TProperty">Type of property.</typeparam>
    /// <returns>Builder continuation.</returns>
    IChildrenBuilder<TParentModel, TChildModel> IdentifiedBy<TProperty>(Expression<Func<TChildModel, TProperty>> propertyExpression);

    /// <summary>
    /// Build the <see cref="ChildrenDefinition"/>.
    /// </summary>
    /// <returns>A a new <see cref="ChildrenDefinition"/>.</returns>
    ChildrenDefinition Build();
}
