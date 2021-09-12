// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Grpc;

namespace Cratis.Events
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStore"/>.
    /// </summary>
    public class EventStore : IEventStore
    {
        readonly IGrpcChannel _channel;

        public EventStore(IGrpcChannel channel)
        {
            _channel = channel;
        }

        /// <inheritdoc/>
        public IEventLog EventLog(EventLogId eventLogId) => new EventLog(_channel, eventLogId);

        /// <inheritdoc/>
        public Task Commit(EventSourceId eventSourceId, object content)
        {
            return EventLog(Guid.Empty).Commit(eventSourceId, content);
        }
    }
}
