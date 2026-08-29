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
}
