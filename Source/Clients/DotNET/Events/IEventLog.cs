// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Defines the interface for working with an event log.
    /// </summary>
    public interface IEventLog
    {
        /// <summary>
        /// Append an event to the event log.
        /// </summary>
        /// <param name="eventSourceId"><see cref="EventSourceId"/> to append for.</param>
        /// <param name="content">Content of the event.</param>
        /// <returns>Async task.</returns>
        Task Append(EventSourceId eventSourceId, object content);
    }
}
