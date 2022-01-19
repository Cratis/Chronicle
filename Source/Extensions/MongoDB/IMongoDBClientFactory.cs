// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Aksio.Cratis.Extensions.MongoDB
{
    /// <summary>
    /// Defines a factory that is capable of creating <see cref="IMongoClient"/> instances.
    /// </summary>
    public interface IMongoDBClientFactory
    {
        /// <summary>
        /// Create a new <see cref="IMongoClient"/> from <see cref="MongoClientSettings"/>.
        /// </summary>
        /// <param name="settings"><see cref="MongoClientSettings"/> to use.</param>
        /// <returns>A new <see cref="IMongoClient"/>.</returns>
        IMongoClient Create(MongoClientSettings settings);

        /// <summary>
        /// Create a new <see cref="IMongoClient"/> from <see cref="MongoUrl"/>.
        /// </summary>
        /// <param name="url"><see cref="MongoUrl"/> to use.</param>
        /// <returns>A new <see cref="IMongoClient"/>.</returns>
        IMongoClient Create(MongoUrl url);

        /// <summary>
        /// Create a new <see cref="IMongoClient"/> from a connection string.
        /// </summary>
        /// <param name="connectionString">Connection string to use.</param>
        /// <returns>A new <see cref="IMongoClient"/>.</returns>
        IMongoClient Create(string connectionString);
    }
}
