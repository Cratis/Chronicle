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
    public Task Save(Chronicle.Storage.ReadModels.ReplayContext context)
    {
        var storageContext = context.ToMongoDB();
        return _collection.ReplaceOneAsync(_ => _.ReadModel == storageContext.ReadModel, storageContext, new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public Task<Result<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>> TryGet(ReadModelIdentifier readModel)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ReadModel, readModel);
        var context = _collection.Find(filter).FirstOrDefault();
        return context == null ?
            Task.FromResult(Result.Failed<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>(GetContextError.NotFound)) :
            Task.FromResult(Result.Success<Chronicle.Storage.ReadModels.ReplayContext, GetContextError>(context.ToChronicle()));
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelIdentifier readModel)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ReadModel, readModel);
        return _collection.DeleteOneAsync(filter);
    }
}
