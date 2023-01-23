// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Defines a builder for building a composite key with a specific type.
/// </summary>
/// <typeparam name="TKeyType">Type of key to build.</typeparam>
/// <typeparam name="TEvent">Event to build from.</typeparam>
public interface ICompositeKeyBuilder<TKeyType, TEvent> : IKeyBuilder
{
    /// <summary>
    /// Start building the set operation to a target property on the model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="modelPropertyAccessor">Model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    ISetBuilder<TKeyType, TEvent, TProperty, ICompositeKeyBuilder<TKeyType, TEvent>> Set<TProperty>(Expression<Func<TKeyType, TProperty>> modelPropertyAccessor);
}
