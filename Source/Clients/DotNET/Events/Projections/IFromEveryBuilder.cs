// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines the builder for building properties that can be set by all events.
/// </summary>
/// <typeparam name="TModel">Type of model to build for.</typeparam>
public interface IFromEveryBuilder<TModel>
{
    /// <summary>
    /// Start building the set operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>The <see cref="IAllSetBuilder{TModel, TBuilder}"/> to build up the property expressions.</returns>
    IAllSetBuilder<TModel, IFromEveryBuilder<TModel>> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor);

    /// <summary>
    /// Instruct the all definition to include all child projections.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IFromEveryBuilder<TModel> IncludeChildProjections();
}
