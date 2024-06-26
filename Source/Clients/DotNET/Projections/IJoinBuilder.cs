// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building join expressions for relationships.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IJoinBuilder<TModel, TEvent> : IModelPropertiesBuilder<TModel, TEvent, IJoinBuilder<TModel, TEvent>>
{
    /// <summary>
    /// Sets the property that defines the relationship from the models perspective.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    IJoinBuilder<TModel, TEvent> On<TProperty>(Expression<Func<TModel, TProperty>> keyAccessor);

    /// <summary>
    /// Build <see cref="JoinDefinition"/> from the builder.
    /// </summary>
    /// <returns>A new instance of <see cref="JoinDefinition"/>.</returns>
    JoinDefinition Build();
}
