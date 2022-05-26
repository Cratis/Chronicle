// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IPropertyExpressionBuilder"/> for counting.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TProperty">The type of the property we're targetting.</typeparam>
public class CountBuilder<TModel, TEvent, TProperty> : IPropertyExpressionBuilder
{
    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = string.Empty;

    /// <inheritdoc/>
    public string Build() => "$count()";
}
