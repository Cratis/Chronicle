// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Monads;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IReplayContextsStorage"/> for MongoDB.
/// </summary>
/// <param name="database">The <see cref="IEventStoreNamespaceDatabase"/> to use.</param>
public class ReplayContextsStorage(IEventStoreNamespaceDatabase database) : IReplayContextsStorage
{
    readonly IMongoCollection<ReplayContext> _collection = database.GetCollection<ReplayContext>(WellKnownCollectionNames.ReplayContexts);

    /// <inheritdoc/>
    public Task Save(Chronicle.Storage.Sinks.ReplayContext context)
    {
        var storageContext = context.ToStorage();
        return _collection.ReplaceOneAsync(_ => _.ModelName == storageContext.ModelName, storageContext, new ReplaceOptions { IsUpsert = true });
    }

    /// <inheritdoc/>
    public Task<Result<Chronicle.Storage.Sinks.ReplayContext, GetContextError>> TryGet(ReadModelName model)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ModelName, model);
        var context = _collection.Find(filter).FirstOrDefault();
        return context == null ?
            Task.FromResult(Result.Failed<Chronicle.Storage.Sinks.ReplayContext, GetContextError>(GetContextError.NotFound)) :
            Task.FromResult(Result.Success<Chronicle.Storage.Sinks.ReplayContext, GetContextError>(context.ToChronicle()));
    }

    /// <inheritdoc/>
    public Task Remove(ReadModelName model)
    {
        var filter = Builders<ReplayContext>.Filter.Eq(_ => _.ModelName, model);
        return _collection.DeleteOneAsync(filter);
    }
}
