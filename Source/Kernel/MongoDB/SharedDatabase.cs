// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISharedDatabase"/>.
/// </summary>
public class SharedDatabase : ISharedDatabase
{
    readonly Dictionary<MicroserviceId, IMongoDatabase> _databases = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Storage _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedDatabase"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for getting current execution context.</param>
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
        var database = GetDatabase();
        if (collectionName == null)
        {
            return database.GetCollection<T>();
        }

        return database.GetCollection<T>(collectionName);
    }

    IMongoDatabase GetDatabase()
    {
        var executionContext = _executionContextManager.Current;
        var microserviceId = executionContext.MicroserviceId;
        if (_databases.ContainsKey(microserviceId))
        {
            return _databases[microserviceId];
        }

        var storageForMicroservice = _configuration[executionContext.MicroserviceId];
        var url = new MongoUrl(storageForMicroservice.Get(WellKnownStorageTypes.EventStore).Shared.ToString());
        var client = _clientFactory.Create(url);
        return _databases[microserviceId] = client.GetDatabase(url.DatabaseName);
    }
}
