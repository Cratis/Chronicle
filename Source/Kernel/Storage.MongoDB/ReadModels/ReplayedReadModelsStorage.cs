// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReplayedReadModelsStorage"/>.
/// </summary>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for the storage.</param>
public class ReplayedReadModelsStorage(IEventStoreNamespaceDatabase database) : IReplayedReadModelsStorage
{
    readonly IMongoCollection<ReplayedReadModel> _collection = database.GetCollection<ReplayedReadModel>(WellKnownCollectionNames.ReplayedReadModels);

    /// <inheritdoc/>
    public async Task Replayed(Chronicle.Storage.ReadModels.ReadModelOccurrence occurrence)
    {
        var filter = Builders<ReplayedReadModel>.Filter.Eq(r => r.ReadModel, occurrence.Type.Identifier);
        var update = Builders<ReplayedReadModel>.Update.Push(r => r.Occurrences, occurrence.ToMongoDB());

        var options = new UpdateOptions { IsUpsert = true };

        await _collection.UpdateOneAsync(filter, update, options);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Chronicle.Storage.ReadModels.ReadModelOccurrence>> GetOccurrences(ReadModelIdentifier readModel)
    {
        var filter = Builders<ReplayedReadModel>.Filter.Eq(r => r.ReadModel, readModel);
        var result = await _collection.Find(filter).FirstOrDefaultAsync();
        return result?.Occurrences.Select(o => o.ToKernel(readModel)) ?? [];
    }
}
