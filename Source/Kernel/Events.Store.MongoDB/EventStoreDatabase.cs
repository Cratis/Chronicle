// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Cratis.Execution;
using Cratis.Extensions.MongoDB;
using Microsoft.Extensions.DependencyInjection;
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
        readonly IServiceProvider _serviceProvider;
        readonly IExecutionContextManager _executionContextManager;
        Dictionary<TenantId, IMongoDatabase> _databases = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
        /// </summary>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/> as a service locator.</param>
        /// <param name="executionContextManager"><see cref="ExecutionContext"/> the database is for.</param>
        public EventStoreDatabase(
            IServiceProvider serviceProvider,
            IExecutionContextManager executionContextManager)
        {
            _serviceProvider = serviceProvider;
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

        IMongoDatabase Database
        {
            get
            {
                if (_databases.Count == 0)
                {
                    var mongoDBClientFactory = _serviceProvider.GetService<IMongoDBClientFactory>()!;
                    var configuration = _serviceProvider.GetService<Storage>()!;
                    _databases = configuration.EventStore.ToDictionary(_ => (TenantId)_.Key, _ =>
                    {
                        var url = new MongoUrl(_.Value.ToString());
                        var client = mongoDBClientFactory.Create(url);
                        return client.GetDatabase(url.DatabaseName);
                    });
                }

                return _databases[_executionContextManager.Current.TenantId];
            }
        }
    }
}
