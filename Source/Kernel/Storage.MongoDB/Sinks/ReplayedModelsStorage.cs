// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IReplayedModelsStorage"/>.
/// </summary>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
public class ReplayedModelsStorage(IEventStoreNamespaceDatabase database) : IReplayedModelsStorage
{
    readonly IMongoCollection<ReplayedModel> _collection = database.GetCollection<ReplayedModel>(WellKnownCollectionNames.ReplayedModels);

    /// <inheritdoc/>
    public async Task Replayed(Chronicle.Storage.Sinks.ReplayContext context)
    {
        var filter = Builders<ReplayedModel>.Filter.Eq(r => r.Model, context.Model);

        var update = Builders<ReplayedModel>.Update.Push(r => r.Occurrences, new ReplayedModelOccurrence(context.Started, context.RevertModel));

        var options = new UpdateOptions { IsUpsert = true };

        await _collection.UpdateOneAsync(filter, update, options);
    }
}
