// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Keys;

/// <summary>
/// Represents an implementation of <see cref="IKeyBuilder{TEvent}"/>.
/// </summary>
/// <typeparam name="TEvent">Type of event to build for.</typeparam>
public class KeyBuilder<TEvent> : IKeyBuilder<TEvent>
{
    /// <inheritdoc/>
    public void UsingCompositeKey<TKeyType>(Action<Projections.ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public void UsingKeyFromContext<TProperty>(Expression<Func<TEvent, EventContext>> keyAccessor) => throw new NotImplementedException();
}
