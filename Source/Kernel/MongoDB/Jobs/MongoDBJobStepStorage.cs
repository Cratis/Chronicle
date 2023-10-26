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
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
public class MongoDBJobStepStorage : IJobStepStorage
{
    readonly ProviderFor<IEventStoreDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStepStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    /// <summary>
    /// Gets the <see cref="IMongoCollection{T}"/> for the job steps.
    /// </summary>
    protected IMongoCollection<BsonDocument> Collection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.JobSteps);

    /// <summary>
    /// Gets the <see cref="IMongoCollection{T}"/> for the failed job steps.
    /// </summary>
    protected IMongoCollection<BsonDocument> FailedCollection => _databaseProvider().GetCollection<BsonDocument>(WellKnownCollectionNames.FailedJobSteps);

    /// <inheritdoc/>
    public async Task RemoveAllForJob(JobId jobId)
    {
        await Collection.DeleteOneAsync(GetJobIdFilter(jobId)).ConfigureAwait(false);
        await FailedCollection.DeleteOneAsync(GetJobIdFilter(jobId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Remove(JobId jobId, JobStepId jobStepId)
    {
        await Collection.DeleteOneAsync(GetIdFilter(jobId, jobStepId)).ConfigureAwait(false);
        await FailedCollection.DeleteOneAsync(GetIdFilter(jobId, jobStepId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<JobStepState>> GetForJob(JobId jobId)
    {
        var filter = GetJobIdFilter(jobId);
        var cursor = Collection.FindAsync(filter).ConfigureAwait(false);


    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<JobStepState>> ObserveForJob(JobId jobId)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get the id filter for a given <see cref="JobId"/> and <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="jobId">Identifier of the job.</param>
    /// <returns><see cref="FilterDefinition{T}"/> for the BsonDocument.</returns>
    protected FilterDefinition<BsonDocument> GetJobIdFilter(Guid jobId) =>
        Builders<BsonDocument>.Filter.Eq(
            new StringFieldDefinition<BsonDocument, BsonDocument>("_id"),
            new BsonDocument
            {
                {
                    "jobId",
                    new BsonBinaryData(jobId, GuidRepresentation.Standard)
                }
            });

    /// <summary>
    /// Get the id filter for a given <see cref="JobId"/> and <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="jobId">Identifier of the job.</param>
    /// <param name="jobStepId">Identifier of the job step.</param>
    /// <returns><see cref="FilterDefinition{T}"/> for the BsonDocument.</returns>
    protected FilterDefinition<BsonDocument> GetIdFilter(Guid jobId, Guid jobStepId) =>
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

/// <summary>
/// Represents an implementation of <see cref="IJobStorage{TJobState}"/> for MongoDB.
/// </summary>
/// <typeparam name="TJobStepState">Type of <see cref="JobStepState"/> to work with.</typeparam>
public class MongoDBJobStepStorage<TJobStepState> : MongoDBJobStepStorage, IJobStepStorage<TJobStepState>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDBJobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreDatabase"/> for persistence.</param>
    public MongoDBJobStepStorage(ProviderFor<IEventStoreDatabase> databaseProvider)
        : base(databaseProvider)
    {
    }

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
    public async Task Save(JobId jobId, JobStepId jobStepId, TJobStepState state)
    {
        var filter = GetIdFilter(jobId, jobStepId);
        var jobStepState = (state as JobStepState)!;
        var collection = jobStepState.Status == JobStepStatus.Failed ? FailedCollection : Collection;
        await collection.ReplaceOneAsync(filter, state.ToBsonDocument(), new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }
}
