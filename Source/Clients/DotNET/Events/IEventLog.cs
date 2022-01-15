// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Defines the client event log.
    /// </summary>
    public interface IEventLog
    {
        /// <summary>
        /// Append a single event to the event store.
        /// </summary>
        /// <typeparam name="T">Type of event to append.</typeparam>
        /// <param name="eventSourceId">The <see cref="EventSourceId"/> to append for.</param>
        /// <param name="event">The event.</param>
        /// <returns>Awaitable <see cref="Task"/>.</returns>
        Task Append<T>(EventSourceId eventSourceId, T @event);
    }
}
