// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Kernel.Persistence.Jobs;
using Aksio.DependencyInversion;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage{TJobState}"/> for MongoDB.
/// </summary>
/// <typeparam name="TJobState">Type of <see cref="JobState"/> to work with.</typeparam>
public class MongoDBJobStorage<TJobState> : IJobStorage<TJobState>
{
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<TJobState?> Read(JobId jobId)
    {
        var filter = GetIdFilter(jobId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.FirstOrDefault();
        if (state is not null)
        {
            return BsonSerializer.Deserialize<TJobState>(state);
        }

        return default!;
    }

    /// <inheritdoc/>
    public Task Remove(JobId jobId)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Save(JobId jobId, TJobState state)
    {
        var filter = GetIdFilter(jobId);
        await Collection.ReplaceOneAsync(filter, state.ToBsonDocument(), new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid id) => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, Guid>("_id"), id);
}
