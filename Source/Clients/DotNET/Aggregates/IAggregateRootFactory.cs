// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Defines a factory that can create instances of <see cref="IAggregateRoot"/>.
/// </summary>
public interface IAggregateRootFactory
{
    /// <summary>
    /// Get an instance of an <see cref="IAggregateRoot"/> for a specific <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="id"><see cref="EventSourceId"/> to get.</param>
    /// <typeparam name="T">Type of <see cref="IAggregateRoot"/> to get.</typeparam>
    /// <returns>The aggregate root instance.</returns>
    /// <remarks>
    /// If the aggregate has event handler methods, the events for the specified <see cref="EventSourceId"/>
    /// will be retrieved and the event handler methods will be invoked.
    /// </remarks>
    Task<T> Get<T>(EventSourceId id)
        where T : IAggregateRoot;
}
