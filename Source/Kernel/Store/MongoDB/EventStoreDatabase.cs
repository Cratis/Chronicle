// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Events.Store.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IEventStoreDatabase"/>.
/// </summary>
[SingletonPerMicroserviceAndTenant]
public class EventStoreDatabase : IEventStoreDatabase
{
    const string EventLogCollectionName = "event-log";
    const string OutboxCollectionName = "outbox";
    const string InboxCollectionName = "inbox";

    readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventStoreDatabase"/> class.
    /// </summary>
    /// <param name="executionContext"><see cref="ExecutionContext"/> the database is for.</param>
    /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> for creating clients.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public EventStoreDatabase(
        ExecutionContext executionContext,
        IMongoDBClientFactory mongoDBClientFactory,
        Storage configuration)
    {
        var storageTypes = configuration.Microservices
                                .Get(executionContext.MicroserviceId).Tenants
                                .Get(executionContext.TenantId);
        var eventStoreForTenant = storageTypes.Get(WellKnownStorageTypes.EventStore);
        var url = new MongoUrl(eventStoreForTenant.ConnectionDetails.ToString());
        var client = mongoDBClientFactory.Create(url);
        _database = client.GetDatabase(url.DatabaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? name = null) => name == null ? _database.GetCollection<T>() : _database.GetCollection<T>(name);

    /// <inheritdoc/>
    public IMongoCollection<Event> GetEventSequenceCollectionFor(EventSequenceId eventSequenceId)
    {
        var collectionName = EventLogCollectionName;
        if (!eventSequenceId.IsEventLog)
        {
            if (eventSequenceId.IsOutbox)
            {
                collectionName = OutboxCollectionName;
            }
            else
            {
                collectionName = InboxCollectionName;
            }
        }

        return _database.GetCollection<Event>(collectionName);
    }
}
