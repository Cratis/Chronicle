// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IKernelProjectionCompositeKeyBuilder{TReadModel}"/>.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
public class KernelProjectionCompositeKeyBuilder<TReadModel> : IKernelProjectionCompositeKeyBuilder<TReadModel>
    where TReadModel : class
{
    readonly List<string> _components = [];

    /// <summary>
    /// Gets a value indicating whether any component has been added.
    /// </summary>
    public bool HasComponents => _components.Count > 0;

    /// <inheritdoc/>
    public IKernelProjectionCompositeKeyBuilder<TReadModel> With<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, PropertyExpression expression)
    {
        var path = KernelProjectionPropertyPathResolver.Resolve(propertyAccessor);
        _components.Add($"{path}={expression.Value}");
        return this;
    }

    /// <summary>
    /// Build the composite key expression.
    /// </summary>
    /// <returns>The composite <see cref="PropertyExpression"/>.</returns>
    /// <exception cref="KernelProjectionCompositeKeyHasNoComponents">Thrown when no component was added.</exception>
    public PropertyExpression Build()
    {
        if (!HasComponents)
        {
            throw new KernelProjectionCompositeKeyHasNoComponents(typeof(TReadModel));
        }

        return new PropertyExpression($"{WellKnownExpressions.Composite}({string.Join(',', _components)})");
    }
}
