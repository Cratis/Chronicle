// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building join expressions for relationships.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IJoinBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IJoinBuilder<TReadModel, TEvent>>
{
    /// <summary>
    /// Sets the property that defines the relationship from the models perspective.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    IJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor);
}
