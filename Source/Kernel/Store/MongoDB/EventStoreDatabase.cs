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
[Singleton]
public class EventStoreDatabase : IEventStoreDatabase
{
    const string BaseCollectionName = "event-log";
    readonly IServiceProvider _serviceProvider;
    readonly IExecutionContextManager _executionContextManager;
    Dictionary<MicroserviceAndTenant, IMongoDatabase> _databases = new();

    IMongoDatabase Database
    {
        get
        {
            if (_databases.Count == 0)
            {
                var mongoDBClientFactory = _serviceProvider.GetService<IMongoDBClientFactory>()!;
                var configuration = _serviceProvider.GetService<Storage>()!;

                _databases = configuration.SelectMany(_ => _.Value.Select(ms =>
                {
                    var url = new MongoUrl(_.Value.ToString());
                    var client = mongoDBClientFactory.Create(url);
                    return new KeyValuePair<MicroserviceAndTenant, IMongoDatabase>(new MicroserviceAndTenant(_.Key, ms.Key), client.GetDatabase(url.DatabaseName));
                })).ToDictionary(_ => _.Key, _ => _.Value);
            }

            return _databases[new(_executionContextManager.Current.MicroserviceId, _executionContextManager.Current.TenantId)];
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
    public IMongoCollection<Event> GetEventLogCollectionFor(EventSequenceId eventLogId)
    {
        var collectionName = BaseCollectionName;
        if (!eventLogId.IsEventLog)
        {
            if (eventLogId.IsOutbox)
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
}
