// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building properties that can be set by all events.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
public interface IFromEveryBuilder<TReadModel>
{
    /// <summary>
    /// Start building the set operation to a target property on the read model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for defining the target property.</param>
    /// <returns>The <see cref="IAllSetBuilder{TReadModel, TBuilder}"/> to build up the property expressions.</returns>
    IAllSetBuilder<TReadModel, IFromEveryBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor);

    /// <summary>
    /// Instruct the all definition to include all child projections.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IFromEveryBuilder<TReadModel> ExcludeChildProjections();
}
