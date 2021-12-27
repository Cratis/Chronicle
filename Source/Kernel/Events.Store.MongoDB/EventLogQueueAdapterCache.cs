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
    /// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueAdapterCache : IQueueAdapterCache
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly IStreamSubscriptionManager _streamSubscriptionManager;
        readonly ProviderFor<IMongoDatabase> _mongoDatabaseProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueAdapterCache"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="streamSubscriptionManager"><see cref="IStreamSubscriptionManager"/> for getting subscriptions.</param>
        /// <param name="mongoDatabaseProvider">Provider for <see cref="IMongoDatabase"/>.</param>
        public EventLogQueueAdapterCache(
            IExecutionContextManager executionContextManager,
            IStreamSubscriptionManager streamSubscriptionManager,
            ProviderFor<IMongoDatabase> mongoDatabaseProvider)
        {
            _executionContextManager = executionContextManager;
            _streamSubscriptionManager = streamSubscriptionManager;
            _mongoDatabaseProvider = mongoDatabaseProvider;
        }

        /// <inheritdoc/>
        public IQueueCache CreateQueueCache(QueueId queueId) => new EventLogQueueCache(queueId, _executionContextManager, _streamSubscriptionManager, _mongoDatabaseProvider);
    }
}
