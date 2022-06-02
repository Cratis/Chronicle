// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Defines the client representation of the event outbox.
/// </summary>
public interface IEventOutbox
{
    /// <summary>
    /// Append a single event to the event store.
    /// </summary>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
    /// <param name="event">The event.</param>
    /// <returns>Awaitable <see cref="Task"/>.</returns>
    Task Append(EventSourceId eventSourceId, object @event);
}
