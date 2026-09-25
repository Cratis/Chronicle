// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IKernelProjectionFromBuilder{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
public class KernelProjectionFromBuilder<TReadModel> : IKernelProjectionFromBuilder<TReadModel>
    where TReadModel : class
{
    readonly Dictionary<PropertyPath, string> _properties = [];

    /// <summary>
    /// Gets the properties and their expressions the builder has collected.
    /// </summary>
    public IDictionary<PropertyPath, string> Properties => _properties;

    /// <inheritdoc/>
    public IKernelProjectionFromBuilder<TReadModel> Set<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, PropertyExpression expression)
    {
        _properties[KernelProjectionPropertyPathResolver.Resolve(propertyAccessor)] = expression.Value;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionFromBuilder<TReadModel> Count<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor)
    {
        _properties[KernelProjectionPropertyPathResolver.Resolve(propertyAccessor)] = WellKnownExpressions.Count;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionFromBuilder<TReadModel> Increment<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor)
    {
        _properties[KernelProjectionPropertyPathResolver.Resolve(propertyAccessor)] = WellKnownExpressions.Increment;
        return this;
    }

    /// <inheritdoc/>
    public IKernelProjectionFromBuilder<TReadModel> Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor)
    {
        _properties[KernelProjectionPropertyPathResolver.Resolve(propertyAccessor)] = WellKnownExpressions.Decrement;
        return this;
    }
}
