// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Patterns;

/// <summary>
/// Defines a system that keeps pattern capture observing the events of an event store namespace.
/// </summary>
public interface IPatternCapture
{
    /// <summary>
    /// Subscribe pattern capture to every event type registered for an event store namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to capture within.</param>
    /// <param name="namespace">The <see cref="EventStoreNamespaceName"/> to capture within.</param>
    /// <returns>Awaitable task.</returns>
    Task Subscribe(EventStoreName eventStore, EventStoreNamespaceName @namespace);

    /// <summary>
    /// Subscribe pattern capture across every namespace of an event store.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to capture within.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Called when event types are registered. A server that starts against a store whose clients have not connected
    /// yet has no event types to subscribe to, and the ones that arrive afterwards would otherwise go uncaptured
    /// until the next restart - which on a first run is every event the store will ever see.
    /// </remarks>
    Task SubscribeAcrossNamespaces(EventStoreName eventStore);
}
