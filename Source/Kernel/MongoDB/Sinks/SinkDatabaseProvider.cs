// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="ISinkDatabaseProvider"/>.
/// </summary>
[Singleton]
public class SinkDatabaseProvider : ISinkDatabaseProvider
{
    readonly ConcurrentDictionary<string, IMongoDatabase> _databases = new();
    readonly IExecutionContextManager _executionContextManager;
    readonly IMongoDBClientFactory _clientFactory;
    readonly Configuration.Storage _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SinkDatabaseProvider"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with execution context.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for creating MongoDB clients.</param>
    /// <param name="configuration">The <see cref="Configuration.Storage"/> configuration.</param>
    public SinkDatabaseProvider(
        IExecutionContextManager executionContextManager,
        IMongoDBClientFactory clientFactory,
        Configuration.Storage configuration)
    {
        _executionContextManager = executionContextManager;
        _clientFactory = clientFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public IMongoDatabase GetDatabase()
    {
        var current = _executionContextManager.Current;
        var key = $"{current.MicroserviceId}-{current.TenantId}";
        if (_databases.TryGetValue(key, out var database)) return database;

        var readModelsConfig = _configuration.Microservices.Get(current.MicroserviceId).Tenants[current.TenantId.ToString()].Get(WellKnownStorageTypes.ReadModels);
        var url = new MongoUrl(readModelsConfig.ConnectionDetails.ToString());
        var client = _clientFactory.Create(url);
        database = client.GetDatabase(url.DatabaseName);
        _databases[key] = database;
        return database;
    }
}
