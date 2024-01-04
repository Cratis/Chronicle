// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
using Aksio.Cratis.Kernel.Storage;
using Aksio.MongoDB;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="IClusterDatabase"/> for MongoDB.
/// </summary>
public class ClusterStorage : IClusterStorage
{
    readonly IDictionary<EventStore, IEventStoreStorage> _eventStores = new Dictionary<EventStore, IEventStoreStorage>();
    readonly IClusterDatabase _database;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;
    readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterStorage"/> class.
    /// </summary>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    /// <param name="loggerFactory"><see cref="ILoggerFactory"/> for creating loggers.</param>
    public ClusterStorage(
        IMongoDBClientFactory clientFactory,
        Storage configuration,
        ILoggerFactory loggerFactory)
    {
        _database = new ClusterDatabase(clientFactory, configuration);
        _clientFactory = clientFactory;
        _configuration = configuration;
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc/>
    public IEventStoreStorage GetEventStore(EventStore eventStore)
    {
        if (_eventStores.TryGetValue(eventStore, out var storage))
        {
            return storage;
        }

        return _eventStores[eventStore] = new EventStoreStorage(
            _database,
            new EventStoreDatabase(eventStore, _clientFactory, _configuration),
            _loggerFactory);
    }
}
