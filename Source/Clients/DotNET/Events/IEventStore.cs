// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Defines the interface for the event store.
    /// </summary>
    public interface IEventStore : IEventLog
    {
        /// <summary>
        /// Gets a specific <see cref="IEventLog"/> based on its <see cref="EventLogId"/>.
        /// </summary>
        /// <param name="eventLogId"><see cref="EventLogId"/> to get.</param>
        /// <returns><see cref="IEventLog"/> to work with.</returns>
        IEventLog   EventLog(EventLogId eventLogId);
    }
}
