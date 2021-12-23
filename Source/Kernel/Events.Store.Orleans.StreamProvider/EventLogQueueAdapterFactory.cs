// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Orleans.Streams;

namespace Events.Store.Orleans.Streams
{
    public class EventLogQueueAdapterFactory : IQueueAdapterFactory
    {
        /// <inheritdoc/>
        public Task<IQueueAdapter> CreateAdapter() => Task.FromResult<IQueueAdapter>(new EventLogQueueAdapter());

        /// <inheritdoc/>
        public Task<IStreamFailureHandler> GetDeliveryFailureHandler(QueueId queueId) => Task.FromResult<IStreamFailureHandler>(new EventLogStreamFailureHandler());

        /// <inheritdoc/>
        public IQueueAdapterCache GetQueueAdapterCache() => new EventLogQueueAdapterCache();

        /// <inheritdoc/>
        public IStreamQueueMapper GetStreamQueueMapper() => new EventLogStreamQueueMapper(new(), "prefix");
    }
}
