// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Configuration;
using Aksio.Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Aksio.Cratis.MongoDB;

/// <summary>
/// Represents an implementation of <see cref="ISharedDatabase"/>.
/// </summary>
public class SharedDatabase : ISharedDatabase
{
    readonly IMongoDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SharedDatabase"/> class.
    /// </summary>
    /// <param name="executionContext">The current <see cref="ExecutionContext"/>.</param>
    /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
    /// <param name="configuration"><see cref="Storage"/> configuration.</param>
    public SharedDatabase(ExecutionContext executionContext, IMongoDBClientFactory clientFactory, Storage configuration)
    {
        var storageForMicroservice = configuration[executionContext.MicroserviceId];
        var url = new MongoUrl(storageForMicroservice.Get(WellKnownStorageTypes.EventStore).Shared.ToString());
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
