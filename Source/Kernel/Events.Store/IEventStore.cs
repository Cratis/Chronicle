// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events.Store
{
    /// <summary>
    /// Defines the store that holds events.
    /// </summary>
    public interface IEventStore
    {
        /// <summary>
        /// Gets the default <see cref="IEventLog"/>.
        /// </summary>
        IEventLog DefaultEventLog { get; }

        /// <summary>
        /// Gets the public <see cref="IEventLog"/>.
        /// </summary>
        IEventLog PublicEventLog { get; }

        /// <summary>
        /// Get a specific <see cref="IEventLog"/>.
        /// </summary>
        /// <param name="eventLogId"><see cref="EventLogId"/> to get.</param>
        /// <returns>The <see cref="IEventLog"/>.</returns>
        IEventLog GetEventLog(EventLogId eventLogId);
    }
}