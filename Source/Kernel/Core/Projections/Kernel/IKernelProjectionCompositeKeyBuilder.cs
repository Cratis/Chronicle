// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Defines the builder for a composite key on a system projection.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
public interface IKernelProjectionCompositeKeyBuilder<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Add a component to the composite key.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the read model property the component lands on.</param>
    /// <param name="expression">The expression producing the component's value.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionCompositeKeyBuilder<TReadModel> With<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, PropertyExpression expression);
}
