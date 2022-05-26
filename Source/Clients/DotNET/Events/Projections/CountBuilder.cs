// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IPropertyExpressionBuilder"/> for counting.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targeting.</typeparam>
public class CountBuilder<TModel, TEvent, TProperty> : IPropertyExpressionBuilder
{
    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CountBuilder{TModel, TEvent, TProperty}"/> class.
    /// </summary>
    /// <param name="targetProperty">Target property we're building for.</param>
    public CountBuilder(PropertyPath targetProperty)
    {
        TargetProperty = targetProperty;
    }

    /// <inheritdoc/>
    public string Build() => "$count()";
}
