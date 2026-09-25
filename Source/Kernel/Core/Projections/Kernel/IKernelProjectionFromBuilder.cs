// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Defines the builder for what a single event - or every event - contributes to a system projection's read model.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
public interface IKernelProjectionFromBuilder<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Set a read model property from an expression.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the read model property to set.</param>
    /// <param name="expression">The expression producing the value.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionFromBuilder<TReadModel> Set<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, PropertyExpression expression);

    /// <summary>
    /// Count occurrences into a read model property.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the read model property to count into.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionFromBuilder<TReadModel> Count<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor);

    /// <summary>
    /// Increment a read model property.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the read model property to increment.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionFromBuilder<TReadModel> Increment<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor);

    /// <summary>
    /// Decrement a read model property.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the read model property to decrement.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionFromBuilder<TReadModel> Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor);
}
