// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage.Generations;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations;

/// <summary>A single atomic document holds the floor and its complete audit trail.</summary>
/// <param name="collection">The dedicated generation-floor collection.</param>
public sealed class GenerationFloorStorage(IMongoCollection<BsonDocument> collection) : IGenerationFloorStorage
{
    static readonly FilterDefinition<BsonDocument> _id = Builders<BsonDocument>.Filter.Eq("_id", 1);

    /// <inheritdoc/>
    public async Task<GenerationFloor?> Get()
    {
        var document = await collection.Find(_id).FirstOrDefaultAsync();
        return document is null ? null : Decode(document);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GenerationFloor>> GetHistory()
    {
        var document = await collection.Find(_id).FirstOrDefaultAsync();
        return document is null ? [] : document["history"].AsBsonArray.Select(_ => Decode(_.AsBsonDocument)).ToArray();
    }

    /// <inheritdoc/>
    public async Task<GenerationFloorAdvanceResult> Advance(GenerationFloorAdvance advance)
    {
        var result = GenerationFloorExtensions.Compare(await Get(), advance);
        if (result is not null)
        {
            return result;
        }
        var next = advance.Next();
        var encoded = Encode(next);
        if (advance.ExpectedRevision == 0)
        {
            var document = new BsonDocument("_id", 1).AddRange(encoded).Add("history", new BsonArray { Encode(next) });
            try
            {
                await collection.InsertOneAsync(document);
            }
            catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                return new GenerationFloorRevisionConflict(await Get());
            }
        }
        else
        {
            var filter = _id & Builders<BsonDocument>.Filter.Eq("revision", (long)advance.ExpectedRevision)
                & Builders<BsonDocument>.Filter.Lte("minimumStorageProtocol", (int)next.MinimumStorageProtocol.Value)
                & Builders<BsonDocument>.Filter.Lte("minimumWriterProtocol", (int)next.MinimumWriterProtocol.Value);
            var update = new BsonDocument { { "$set", encoded }, { "$push", new BsonDocument("history", Encode(next)) } };
            if (await collection.FindOneAndUpdateAsync(filter, update) is null)
            {
                return new GenerationFloorRevisionConflict(await Get());
            }
        }
        return new GenerationFloorAdvanced(next);
    }

    static BsonDocument Encode(GenerationFloor floor) => new()
    {
        { "minimumStorageProtocol", (int)floor.MinimumStorageProtocol.Value },
        { "minimumWriterProtocol", (int)floor.MinimumWriterProtocol.Value },
        { "requiredCapabilities", new BsonArray(floor.RequiredCapabilities.Select(_ => _.Value)) },
        { "incarnation", new BsonBinaryData(floor.Incarnation.Value, GuidRepresentation.Standard) },
        { "revision", checked((long)floor.Revision) },
        { "advancedAt", floor.AdvancedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture) },
        { "advancedBy", floor.AdvancedBy },
        { "reason", floor.Reason }
    };

    static GenerationFloor Decode(BsonDocument document) => new(
        new(checked((uint)document["minimumStorageProtocol"].AsInt32)),
        new(checked((uint)document["minimumWriterProtocol"].AsInt32)),
        document["requiredCapabilities"].AsBsonArray.Select(_ => new GenerationCapability(_.AsString)).ToArray(),
        new(document["incarnation"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard)),
        checked((ulong)document["revision"].AsInt64),
        DateTimeOffset.Parse(document["advancedAt"].AsString, System.Globalization.CultureInfo.InvariantCulture),
        document["advancedBy"].AsString,
        document["reason"].AsString);
}
