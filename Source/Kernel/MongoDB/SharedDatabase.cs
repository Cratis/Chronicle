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
[SingletonPerMicroservice]
public class SharedDatabase : ISharedDatabase
{
    readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedDatabase"/> class.
    /// </summary>
    /// <param name="executionContext">Current <see cref="ExecutionContext"/>.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public SharedDatabase(
        ExecutionContext executionContext,
        IMongoDBClientFactory clientFactory,
        Storage configuration)
    {
        var url = new MongoUrl(configuration.Microservices.Get(executionContext.MicroserviceId).Shared.Get(WellKnownStorageTypes.EventStore).ConnectionDetails.ToString());
        var client = clientFactory.Create(url);
        _database = client.GetDatabase(url.DatabaseName);
    }

    /// <inheritdoc/>
    public IMongoCollection<T> GetCollection<T>(string? collectionName = null)
    {
        if (collectionName == null)
        {
            return _database.GetCollection<T>();
        }

        return _database.GetCollection<T>(collectionName);
    }
}
