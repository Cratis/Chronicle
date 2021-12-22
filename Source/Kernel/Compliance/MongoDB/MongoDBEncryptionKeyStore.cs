// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Extensions.MongoDB;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents an implementation of <see cref="IEncryptionKeyStore"/> for MongoDB.
    /// </summary>
    public class MongoDBEncryptionKeyStore : IEncryptionKeyStore
    {
        readonly IMongoClient _client;
        readonly IMongoDatabase _database;
        readonly IMongoCollection<EncryptionKeyForIdentifier> _encryptionKeysCollection;

        static MongoDBEncryptionKeyStore()
        {
            BsonSerializer.RegisterSerializer(new EncryptionKeySerializer());
            BsonClassMap.RegisterClassMap<EncryptionKeyForIdentifier>(cm =>
            {
                cm.AutoMap();
                cm.MapIdProperty(_ => _.Identifier);
            });
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBEncryptionKeyStore"/> class.
        /// </summary>
        /// <param name="mongoDBClientFactory"><see cref="IMongoDBClientFactory"/> to use for accessing database.</param>
        public MongoDBEncryptionKeyStore(IMongoDBClientFactory mongoDBClientFactory)
        {
            var mongoUrlBuilder = new MongoUrlBuilder
            {
                Servers = new[] { new MongoServerAddress("localhost", 27017) }
            };
            var url = mongoUrlBuilder.ToMongoUrl();
            var settings = MongoClientSettings.FromUrl(url);
            _client = mongoDBClientFactory.Create(settings);
            _database = _client.GetDatabase("certificates");
            _encryptionKeysCollection = _database.GetCollection<EncryptionKeyForIdentifier>("encryption-keys");
        }

        /// <inheritdoc/>
        public async Task SaveFor(EncryptionKeyIdentifier identifier, EncryptionKey key)
        {
            await _encryptionKeysCollection.ReplaceOneAsync(
                _ => _.Identifier == identifier,
                new EncryptionKeyForIdentifier(identifier, key),
                new ReplaceOptions() { IsUpsert = true });
        }

        /// <inheritdoc/>
        public async Task<bool> HasFor(EncryptionKeyIdentifier identifier)
        {
            var result = await _encryptionKeysCollection.CountDocumentsAsync(_ => _.Identifier == identifier);
            return result == 1;
        }

        /// <inheritdoc/>
        public async Task<EncryptionKey> GetFor(EncryptionKeyIdentifier identifier)
        {
            var result = await _encryptionKeysCollection.FindAsync(_ => _.Identifier == identifier);
            return result.Single().Key;
        }

        /// <inheritdoc/>
        public async Task DeleteFor(EncryptionKeyIdentifier identifier) => await _encryptionKeysCollection.DeleteOneAsync(_ => _.Identifier == identifier);
    }
}
