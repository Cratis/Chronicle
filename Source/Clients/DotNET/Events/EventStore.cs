// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        /// <inheritdoc/>
        public IEventLog EventLog(EventLogId eventLogId) => new EventLog(eventLogId);

        /// <inheritdoc/>
        public Task Append(EventSourceId eventSourceId, object content)
        {
            return EventLog(Guid.Empty).Append(eventSourceId, content);
        }
    }
}
