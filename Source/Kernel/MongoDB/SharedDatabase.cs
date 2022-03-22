// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISharedDatabase"/>.
/// </summary>
[Singleton]
public class SharedDatabase : ISharedDatabase
{
    readonly ConcurrentDictionary<MicroserviceId, IMongoDatabase> _databases = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedDatabase"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public SharedDatabase(
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
    {
        if (collectionName == null)
        {
            return GetDatabase().GetCollection<T>();
        }

        return GetDatabase().GetCollection<T>(collectionName);
    }

    IMongoDatabase GetDatabase()
    {
        var microserviceId = _executionContextManager.Current.MicroserviceId;
        if (_databases.ContainsKey(microserviceId))
        {
            return _databases[microserviceId];
        }

        var url = new MongoUrl(_configuration.Microservices.Get(microserviceId).Shared.Get(WellKnownStorageTypes.EventStore).ConnectionDetails.ToString());
        var client = _clientFactory.Create(url);
        return _databases[microserviceId] = client.GetDatabase(url.DatabaseName);
    }
}
