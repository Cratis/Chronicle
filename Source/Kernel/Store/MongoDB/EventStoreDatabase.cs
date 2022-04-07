// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreDatabase"/>.
/// </summary>
[SingletonPerMicroserviceAndTenant]
public class EventStoreDatabase : IEventStoreDatabase
{
    const string BaseCollectionName = "event-log";
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;
    IMongoDatabase? _database;

    IMongoDatabase Database
    {
        get
        {
            if (_database is null)
            {
                var mongoDBClientFactory = _serviceProvider.GetService<IMongoDBClientFactory>()!;
                var configuration = _serviceProvider.GetService<Storage>()!;

                var microserviceId = _executionContextManager.Current.MicroserviceId;
                var tenantId = _executionContextManager.Current.TenantId;
                var storageTypes = configuration.Microservices.Get(microserviceId).Tenants.Get(tenantId);
                var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);
                var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
                var client = mongoDBClientFactory.Create(url);
                _database = client.GetDatabase(url.DatabaseName);
            }

            return _database;
        }
    }

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
    public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = BaseCollectionName;
        if (!eventSequenceId.IsEventLog)
        {
            if (eventSequenceId.IsOutbox)
            {
                collectionName = $"{BaseCollectionName}-public";
            }
            else
            {
                collectionName = $"{BaseCollectionName}-{eventSequenceId}";
            }
        }

        return Database.GetCollection<Event>(collectionName);
    }
}
