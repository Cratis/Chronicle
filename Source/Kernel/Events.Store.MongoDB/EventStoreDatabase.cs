// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events.Store.Configuration;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;
using ExecutionContext = Cratis.Execution.ExecutionContext;

namespace Cratis.Events.Store.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEventStoreDatabase"/>.
    /// </summary>
    [Singleton]
    public class EventStoreDatabase : IEventStoreDatabase
    {
        const string BaseCollectionName = "event-log";
        readonly Dictionary<TenantId, IMongoDatabase> _databases;
        readonly IExecutionContextManager _executionContextManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="executionContextManager"><see cref="ExecutionContext"/> the database is for.</param>
        /// <param name="configuration"><see cref="Storage"/> configuration.</param>
        public EventStoreDatabase(IMongoDBClientFactory mongoDBClientFactory, IExecutionContextManager executionContextManager, Storage configuration)
        {
            _databases = configuration.EventStore.Configuration.ToDictionary(_ => (TenantId)_.Key, _ =>
            {
                var url = new MongoUrl(_.Value.ToString());
                var client = mongoDBClientFactory.Create(url);
                return client.GetDatabase(url.DatabaseName);
            });

            _executionContextManager = executionContextManager;
        }

        /// <inheritdoc/>
        public IMongoCollection<T> GetCollection<T>(string? name = null) => name == null ? Database.GetCollection<T>() : Database.GetCollection<T>(name);

        /// <inheritdoc/>
        public IMongoCollection<Event> GetEventLogCollectionFor(EventLogId eventLogId)
        {
            var collectionName = BaseCollectionName;
            if (!eventLogId.IsDefault)
            {
                if (eventLogId.IsPublic)
                {
                    collectionName = $"{BaseCollectionName}-public";
                }
                else
                {
                    collectionName = $"{BaseCollectionName}-{eventLogId}";
                }
            }

            return Database.GetCollection<Event>(collectionName);
        }

        IMongoDatabase Database => _databases[_executionContextManager.Current.TenantId];
    }
}
