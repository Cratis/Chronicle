// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInversion;
using Cratis.Execution;
using MongoDB.Driver;
using Orleans.Streams;
using Orleans.Streams.Core;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueCache"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueCache : IQueueCache
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly ProviderFor<IMongoDatabase> _mongoDatabaseProvider;
        readonly QueueId _queueId;
        readonly IStreamSubscriptionManager _streamSubscriptionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueCache"/> class.
        /// </summary>
        /// <param name="queueId"><see cref="QueueId"/> the cache is for.</param>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="streamSubscriptionManager"><see cref="IStreamSubscriptionManager"/> for getting subscriptions.</param>
        /// <param name="mongoDatabaseProvider">Provider for <see cref="IMongoDatabase"/>.</param>
        public EventLogQueueCache(
            QueueId queueId,
            IExecutionContextManager executionContextManager,
            IStreamSubscriptionManager streamSubscriptionManager,
            ProviderFor<IMongoDatabase> mongoDatabaseProvider)
        {
            _executionContextManager = executionContextManager;
            _mongoDatabaseProvider = mongoDatabaseProvider;
            _queueId = queueId;
            _streamSubscriptionManager = streamSubscriptionManager;
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
            var collection = _mongoDatabaseProvider().GetEventLogCollectionFor(streamIdentity.Guid);
            return new EventLogQueueCacheCursor(collection, _streamSubscriptionManager, streamIdentity, token);
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
