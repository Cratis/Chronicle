// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.EventLogs
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueAdapterCache : IQueueAdapterCache
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly IEventLogStorageProvider _eventLogStorageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueAdapterCache"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="eventLogStorageProvider"><see cref="IEventLogStorageProvider"/> for getting events from storage.</param>
        public EventLogQueueAdapterCache(
            IExecutionContextManager executionContextManager,
            IEventLogStorageProvider eventLogStorageProvider)
        {
            _executionContextManager = executionContextManager;
            _eventLogStorageProvider = eventLogStorageProvider;
        }

        /// <inheritdoc/>
        public IQueueCache CreateQueueCache(QueueId queueId) => new EventLogQueueCache(_executionContextManager, _eventLogStorageProvider);
    }
}
