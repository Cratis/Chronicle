// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events;
using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Keys;

/// <summary>
/// Defines a key builder for a specific event.
/// </summary>
/// <typeparam name="TEvent">Type of event the builder is for.</typeparam>
public interface IKeyBuilder<TEvent>
{
    /// <summary>
    /// Define what key to use. This is optional, if not set - it will default to using the event source identifier on the event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    void UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a value in the <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property within <see cref="EventContext"/> to use.</param>
    void UsingKeyFromContext<TProperty>(Expression<Func<TEvent, EventContext>> keyAccessor);

    /// <summary>
    /// Define what key to use based on a composite of expressions.
    /// </summary>
    /// <typeparam name="TKeyType">Type of key.</typeparam>
    /// <param name="builderCallback">Builder callback for building the composite key.</param>
    void UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback);
}
