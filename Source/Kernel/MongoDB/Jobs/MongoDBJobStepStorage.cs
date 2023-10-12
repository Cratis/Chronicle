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
/// <typeparam name="TJobStepState">Type of <see cref="JobState"/> to work with.</typeparam>
public class MongoDBJobStepStorage<TJobStepState> : IJobStepStorage<TJobStepState>
{
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStepStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.JobSteps);
    IMongoCollection<BsonDocument> FailedCollection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.FailedJobSteps);

    /// <inheritdoc/>
    public async Task<TJobStepState?> Read(JobId jobId, JobStepId jobStepId)
    {
        var filter = GetIdFilter(jobId, jobStepId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var state = cursor.FirstOrDefault();
        if (state is not null)
        {
            return BsonSerializer.Deserialize<TJobStepState>(state);
        }

        return default!;
    }

    /// <inheritdoc/>
    public Task Remove(JobId jobId, JobStepId jobStepId)
    {
        var filter = GetIdFilter(jobId, jobStepId);
        return Collection.DeleteOneAsync(filter);
    }

    /// <inheritdoc/>
    public async Task Save(JobId jobId, JobStepId jobStepId, TJobStepState state)
    {
        var filter = GetIdFilter(jobId, jobStepId);
        var jobStepState = (state as JobStepState)!;
        var collection = jobStepState.Status == JobStepStatus.Failed ? FailedCollection : Collection;
        await collection.ReplaceOneAsync(filter, state.ToBsonDocument(), new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    FilterDefinition<BsonDocument> GetIdFilter(Guid jobId, Guid jobStepId) =>
        Builders<BsonDocument>.Filter.Eq(
            new StringFieldDefinition<BsonDocument, BsonDocument>("_id"),
            new BsonDocument
            {
                {
                    "jobId",
                    new BsonBinaryData(jobId, GuidRepresentation.Standard)
                },
                {
                    "jobStepId", new BsonBinaryData(jobStepId, GuidRepresentation.Standard)
                }
            });
}
