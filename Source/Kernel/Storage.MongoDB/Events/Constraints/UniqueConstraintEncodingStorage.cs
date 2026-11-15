// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Chronicle.Storage.Events.Constraints;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Stores the event store's unique-constraint encoding key as one raw document, inserted atomically and never replaced.
/// </summary>
/// <remarks>
/// Works on raw <see cref="BsonDocument"/> so no class map or serializer can ever emit the material.
/// </remarks>
/// <param name="collection">The collection holding the single key document.</param>
public sealed class UniqueConstraintEncodingStorage(IMongoCollection<BsonDocument> collection) : IUniqueConstraintEncodingStorage
{
    const string KeyIdField = "keyId";
    const string MaterialField = "material";
    static readonly FilterDefinition<BsonDocument> _singleton = Builders<BsonDocument>.Filter.Eq("_id", 1);

    /// <inheritdoc/>
    public async Task<UniqueConstraintEncodingKey?> GetKey()
    {
        var document = await collection
            .WithReadConcern(ReadConcern.Majority)
            .WithReadPreference(ReadPreference.Primary)
            .Find(_singleton)
            .FirstOrDefaultAsync();
        return document is null ? null : Decode(document);
    }

    /// <inheritdoc/>
    public async Task<UniqueConstraintEncodingKey> CreateKeyIfAbsent(UniqueConstraintEncodingKey candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var keyId = UniqueConstraintStoreKeyIdBytes.From(candidate.KeyId);
        var material = candidate.Material.CopyBytes();
        try
        {
            var document = new BsonDocument
            {
                { "_id", 1 },
                { KeyIdField, new BsonBinaryData(keyId, BsonBinarySubType.Binary) },
                { MaterialField, new BsonBinaryData(material, BsonBinarySubType.Binary) }
            };
            await collection.WithWriteConcern(WriteConcern.WMajority).InsertOneAsync(document);
            return candidate;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return await GetKey() ?? throw new InvalidOperationException("The unique-constraint encoding key could not be read after a concurrent insert.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }

    static UniqueConstraintEncodingKey Decode(BsonDocument document)
    {
        var keyId = document[KeyIdField].AsBsonBinaryData.Bytes;
        var material = document[MaterialField].AsBsonBinaryData.Bytes;
        try
        {
            return new(UniqueConstraintStoreKeyIdBytes.To(keyId), new UniqueConstraintStoreKey(material));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(material);
        }
    }
}
