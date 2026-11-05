// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Moves the content of one MongoDB database into another, then drops the source.
/// </summary>
/// <remarks>
/// MongoDB cannot rename a database, and renaming a collection across databases is neither available everywhere nor
/// possible across servers, so collections are copied: their creation options, their indexes and their documents.
/// The target may already hold documents written before the move — a document already in the target is kept, since
/// it was written by the running kernel. Indexes the target already has under the same name are kept for the same
/// reason. The source is dropped only after every collection has been copied.
/// </remarks>
internal static class MongoDatabaseMove
{
    const int BatchSize = 1000;

    static readonly int[] _existingIndexCodes = [68, 85, 86];

    /// <summary>
    /// Copy every collection of a database, with its indexes, into another database, and drop the source once the
    /// copy is confirmed.
    /// </summary>
    /// <param name="source">The <see cref="IMongoDatabase"/> to move from.</param>
    /// <param name="target">The <see cref="IMongoDatabase"/> to move to.</param>
    /// <param name="logger">The <see cref="ILogger"/> for logging.</param>
    /// <returns>The <see cref="DatabaseMoveOutcome"/>: moved, kept because the copy could not be confirmed, or nothing to move.</returns>
    public static async Task<DatabaseMoveOutcome> Move(IMongoDatabase source, IMongoDatabase target, ILogger logger)
    {
        var filter = new BsonDocument("type", "collection");
        var collections = (await (await source.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter })).ToListAsync())
            .Where(_ => !_["name"].AsString.StartsWith("system.", StringComparison.Ordinal))
            .ToArray();
        if (collections.Length == 0)
        {
            return DatabaseMoveOutcome.NothingToMove;
        }

        var existing = (await (await target.ListCollectionNamesAsync()).ToListAsync()).ToHashSet(StringComparer.Ordinal);
        foreach (var collection in collections)
        {
            var name = collection["name"].AsString;
            if (!existing.Contains(name))
            {
                var create = new BsonDocument("create", name);
                if (collection.TryGetValue("options", out var options) && options.IsBsonDocument)
                {
                    create.AddRange(options.AsBsonDocument);
                }
                await target.RunCommandAsync<BsonDocument>(create);
            }

            await CopyIndexes(source.GetCollection<BsonDocument>(name), target, name, logger);
            await CopyDocuments(source.GetCollection<BsonDocument>(name), target.GetCollection<BsonDocument>(name));
        }

        // The source is the only copy of anything that did not make it across, so it is dropped only once every
        // collection is confirmed to be in the target.
        var confirmed = true;
        foreach (var collection in collections)
        {
            confirmed &= await IsCopyConfirmed(source, target, collection["name"].AsString, logger);
        }

        if (!confirmed)
        {
            return DatabaseMoveOutcome.Kept;
        }

        await source.Client.DropDatabaseAsync(source.DatabaseNamespace.DatabaseName);
        return DatabaseMoveOutcome.Moved;
    }

    static async Task<bool> IsCopyConfirmed(IMongoDatabase source, IMongoDatabase target, string name, ILogger logger)
    {
        var sourceCount = await source.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);

        // The target can hold more than the source, documents the running kernel wrote there, but never fewer: every
        // document of the source is either copied or was already there.
        var targetCount = await target.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        if (targetCount >= sourceCount)
        {
            return true;
        }

        logger.UnconfirmedCopy(name, source.DatabaseNamespace.DatabaseName, target.DatabaseNamespace.DatabaseName, sourceCount, targetCount);
        return false;
    }

    static async Task CopyIndexes(IMongoCollection<BsonDocument> source, IMongoDatabase target, string name, ILogger logger)
    {
        var indexes = await (await source.Indexes.ListAsync()).ToListAsync();
        foreach (var index in indexes.Where(_ => _["name"].AsString != "_id_"))
        {
            index.Remove("v");
            index.Remove("ns");
            try
            {
                await target.RunCommandAsync<BsonDocument>(new BsonDocument
                {
                    { "createIndexes", name },
                    { "indexes", new BsonArray { index } }
                });
            }
            catch (MongoCommandException exception) when (_existingIndexCodes.Contains(exception.Code))
            {
                // The target already has this index, created by the running kernel, which knows the shape it wants.
                logger.KeepingExistingIndex(index["name"].AsString, name, target.DatabaseNamespace.DatabaseName);
            }
        }
    }

    static async Task CopyDocuments(IMongoCollection<BsonDocument> source, IMongoCollection<BsonDocument> target)
    {
        using var cursor = await source.FindAsync(FilterDefinition<BsonDocument>.Empty, new FindOptions<BsonDocument> { BatchSize = BatchSize });
        while (await cursor.MoveNextAsync())
        {
            var batch = cursor.Current.ToArray();
            if (batch.Length == 0)
            {
                continue;
            }

            try
            {
                await target.InsertManyAsync(batch, new InsertManyOptions { IsOrdered = false });
            }
            catch (MongoBulkWriteException<BsonDocument> exception) when (exception.WriteErrors.All(_ => _.Category == ServerErrorCategory.DuplicateKey) && exception.WriteConcernError is null)
            {
                // Documents already in the target were written by the running kernel before the move and are kept.
            }
        }
    }
}
