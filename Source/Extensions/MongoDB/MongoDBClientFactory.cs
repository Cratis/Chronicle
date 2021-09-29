// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;
using MongoDB.Driver;

namespace Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IMongoDBClientFactory"/>.
    /// </summary>
    [Singleton]
    public class MongoDBClientFactory : IMongoDBClientFactory
    {
        /// <inheritdoc/>
        public IMongoClient Create(MongoClientSettings settings) => new MongoClient(settings);

        /// <inheritdoc/>
        public IMongoClient Create(MongoUrl url) => new MongoClient(url);

        /// <inheritdoc/>
        public IMongoClient Create(string connectionString) => new MongoClient(connectionString);
    }
}
