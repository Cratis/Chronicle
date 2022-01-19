// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events.Store.Grains;
using Aksio.Cratis.Execution;
using MongoDB.Driver;
using Orleans.Streams;

namespace Aksio.Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueCache : IQueueCache
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly IEventStoreDatabase _eventStoreDatabase;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueCache"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="eventStoreDatabase">Provider for <see cref="IMongoDatabase"/>.</param>
        public EventLogQueueCache(
            IExecutionContextManager executionContextManager,
            IEventStoreDatabase eventStoreDatabase)
        {
            _executionContextManager = executionContextManager;
            _eventStoreDatabase = eventStoreDatabase;
        }

        /// <inheritdoc/>
        public void AddToCache(IList<IBatchContainer> messages)
        {
        }

        /// <inheritdoc/>
        public IQueueCacheCursor GetCacheCursor(IStreamIdentity streamIdentity, StreamSequenceToken token)
        {
            var tenantId = (TenantId)streamIdentity.Namespace;
            _executionContextManager.Establish(tenantId, CorrelationId.New());
            var collection = _eventStoreDatabase.GetEventLogCollectionFor(streamIdentity.Guid);

            if (token is EventLogSequenceNumberTokenWithFilter tokenWithFilter)
            {
                return new EventLogQueueCacheCursor(collection, streamIdentity, token, tokenWithFilter.EventTypes, tokenWithFilter.Partition);
            }

            return new EventLogQueueCacheCursor(collection, streamIdentity, token);
        }

        /// <inheritdoc/>
        public int GetMaxAddCount() => int.MaxValue;

        /// <inheritdoc/>
        public bool IsUnderPressure() => false;

        /// <inheritdoc/>
        public bool TryPurgeFromCache(out IList<IBatchContainer> purgedItems)
        {
            purgedItems = new List<IBatchContainer>();
            return true;
        }
    }
}
