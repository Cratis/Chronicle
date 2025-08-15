// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents an implementation of <see cref="IReplayedModelsStorage"/>.
/// </summary>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
public class ReplayedModelsStorage(IEventStoreNamespaceDatabase database) : IReplayedModelsStorage
{
    readonly IMongoCollection<ReplayedModel> _collection = database.GetCollection<ReplayedModel>(WellKnownCollectionNames.ReplayedReadModels);

    /// <inheritdoc/>
    public async Task Replayed(ObserverId observer, Chronicle.Storage.Sinks.ReplayContext context)
    {
        var filter = Builders<ReplayedModel>.Filter.Eq(r => r.ReadModel, context.ReadModel) &
                     Builders<ReplayedModel>.Filter.Eq(r => r.Observer, observer);

        var update = Builders<ReplayedModel>.Update.Push(r => r.Occurrences, new ReplayedModelOccurrence(context.Started, context.RevertModel));

        var options = new UpdateOptions { IsUpsert = true };

        await _collection.UpdateOneAsync(filter, update, options);
    }
}
