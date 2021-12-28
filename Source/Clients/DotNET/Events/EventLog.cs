// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventLog"/>.
    /// </summary>
    public class EventLog : IEventLog
    {
        readonly EventLogId _eventLogId;

        public EventLog(EventLogId eventLogId)
        {
            _eventLogId = eventLogId;
        }

        /// <inheritdoc/>
        public Task Append(EventSourceId eventSourceId, object content)
        {
            return Task.CompletedTask;
        }
    }
}
