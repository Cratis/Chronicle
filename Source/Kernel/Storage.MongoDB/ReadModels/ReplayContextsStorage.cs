// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayContextsStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
public class ReplayContextsStorage(IEventStoreNamespaceDatabase database) : IReplayContextsStorage
{
    readonly IMongoCollection<ReplayContext> _collection = database.GetCollection<ReplayContext>(WellKnownCollectionNames.ReplayContexts);

    /// <inheritdoc/>
    public async Task Save(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        var storageContext = context.ToMongoDB();
        var options = new ReplaceOptions { IsUpsert = true };
        try
        {
            await _collection.ReplaceOneAsync(_ => _.ReadModel == storageContext.ReadModel, storageContext, options);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            // Every silo establishes the context when a replay begins, so two upserts of the same document
            // can race: both find nothing to replace and both insert, and the second fails on the key. The
            // document exists now, so the retry replaces it rather than inserting (#4296).
            await _collection.ReplaceOneAsync(_ => _.ReadModel == storageContext.ReadModel, storageContext, options);
        }
    }

    /// <inheritdoc/>
    public async Task<Result<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ReadModel, readModel);
        var context = await _collection.Find(filter).FirstOrDefaultAsync();
        return context == null ?
            Result.Failed<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>(GetContextError.NotFound) :
            Result.Success<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>(context.ToChronicle());
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelIdentifier readModel)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ReadModel, readModel);
        return _collection.DeleteOneAsync(filter);
    }
}
