// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Configuration;
using Cratis.Extensions.MongoDB;
using MongoDB.Driver;

namespace Cratis.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="ISharedDatabase"/>.
    /// </summary>
    public class SharedDatabase : ISharedDatabase
    {
        readonly IMongoDatabase _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedDatabase"/> class.
        /// </summary>
        /// <param name="clientFactory"><see cref="IMongoDBClientFactory"/> for working with MongoDB.</param>
        /// <param name="configuration"></param>
        public SharedDatabase(IMongoDBClientFactory clientFactory, Storage configuration)
        {
            var url = new MongoUrl(configuration.Shared.ToString());
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
}
