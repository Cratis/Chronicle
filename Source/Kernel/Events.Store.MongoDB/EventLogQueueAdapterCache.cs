// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInversion;
using Cratis.Execution;
using MongoDB.Driver;
using Orleans.Streams;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IQueueAdapterCache"/> for MongoDB event log.
    /// </summary>
    public class EventLogQueueAdapterCache : IQueueAdapterCache
    {
        readonly IExecutionContextManager _executionContextManager;
        readonly ProviderFor<IMongoDatabase> _mongoDatabaseProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLogQueueAdapterCache"/> class.
        /// </summary>
        /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
        /// <param name="mongoDatabaseProvider"></param>
        public EventLogQueueAdapterCache(
            IExecutionContextManager executionContextManager,
            ProviderFor<IMongoDatabase> mongoDatabaseProvider)
        {
            _executionContextManager = executionContextManager;
            _mongoDatabaseProvider = mongoDatabaseProvider;
        }

        /// <inheritdoc/>
        public IQueueCache CreateQueueCache(QueueId queueId) => new EventLogQueueCache(queueId, _executionContextManager, _mongoDatabaseProvider);
    }
}
