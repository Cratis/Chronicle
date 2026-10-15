// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences;

/// <summary>
/// Stores trigger definitions in the event-store database with revision compare-and-set.
/// </summary>
/// <param name="database">The event-store database.</param>
public class TriggerDefinitionsStorage(IMongoDatabase database) : ITriggerDefinitionsStorage
{
    readonly IMongoCollection<BsonDocument> _definitions = database.GetCollection<BsonDocument>("trigger-definitions");

    /// <inheritdoc/>
    public async Task<TriggerDefinition?> Get(TriggerId id)
    {
        var document = await _definitions.Find(new BsonDocument("_id", id.Value)).SingleOrDefaultAsync();
        return document is null ? null : FromDocument(document);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<TriggerDefinition>> GetAll()
    {
        var documents = await _definitions.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return documents.Select(FromDocument).ToArray();
    }

    /// <inheritdoc/>
    public async Task<bool> TrySave(TriggerDefinition definition, long? expectedRevision)
    {
        var document = new BsonDocument { { "_id", definition.Id.Value }, { "name", definition.Name }, { "schema", definition.Schema }, { "revision", definition.Revision } };
        if (expectedRevision.HasValue)
        {
            var filter = new BsonDocument { { "_id", definition.Id.Value }, { "revision", expectedRevision.Value } };
            var result = await _definitions.ReplaceOneAsync(filter, document);
            return result.MatchedCount == 1;
        }

        try
        {
            await _definitions.InsertOneAsync(document);
            return true;
        }
        catch (MongoWriteException error) when (error.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    static TriggerDefinition FromDocument(BsonDocument document) => new(new TriggerId(document["_id"].AsString), document["name"].AsString, document["schema"].AsString, document["revision"].ToInt64());
}
