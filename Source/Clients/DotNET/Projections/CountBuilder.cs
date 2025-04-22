// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IPropertyExpressionBuilder"/> for counting.
/// </summary>
/// <param name="targetProperty">Target property we're building for.</param>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
public class CountBuilder<TModel, TEvent, TProperty>(PropertyPath targetProperty) : IPropertyExpressionBuilder
{
    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public string Build() => "$count()";
}
