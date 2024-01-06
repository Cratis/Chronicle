// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.Cratis.Kernel.Storage.MongoDB.Observation;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
public class JobStepStorage : IJobStepStorage
{
    readonly IEventStoreNamespaceDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage"/> class.
    /// </summary>
    /// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for persistence.</param>
    public JobStepStorage(IEventStoreNamespaceDatabase database)
    {
        _database = database;
    }

    IMongoCollection<JobStepState> Collection => _database.GetCollection<JobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<JobStepState> FailedCollection => _database.GetCollection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);

    /// <inheritdoc/>
    public async Task RemoveAllForJob(JobId jobId)
    {
        await Collection.DeleteOneAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
        await FailedCollection.DeleteOneAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task RemoveAllNonFailedForJob(JobId jobId)
    {
        await Collection.DeleteOneAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task Remove(JobId jobId, JobStepId jobStepId)
    {
        await Collection.DeleteOneAsync(GetIdFilter<JobStepState>(jobId, jobStepId)).ConfigureAwait(false);
        await FailedCollection.DeleteOneAsync(GetIdFilter<JobStepState>(jobId, jobStepId)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobStepState>> GetForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        var failedJobSteps = new List<JobStepState>();

        var filter = GetJobIdFilter<JobStepState>(jobId);
        if (statuses.Any(_ => _ == JobStepStatus.Failed) || statuses.Length == 0)
        {
            var failedCursor = await FailedCollection.FindAsync(filter).ConfigureAwait(false);
            failedJobSteps = failedCursor.ToList();

            if (statuses.Any(_ => _ != JobStepStatus.Failed))
            {
                return failedJobSteps.ToImmutableList();
            }
        }

        if (statuses.Length > 0)
        {
            filter &= Builders<JobStepState>.Filter.In(nameof(JobStepState.Status).ToCamelCase(), statuses);
        }

        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var jobsSteps = cursor.ToList();
        return jobsSteps.Concat(failedJobSteps).ToImmutableList();
    }

    /// <inheritdoc/>
    public async Task<int> CountForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        var filter = GetJobIdFilter<JobStepState>(jobId);
        var count = 0L;
        if (statuses.Any(_ => _ == JobStepStatus.Failed) || statuses.Length == 0)
        {
            count = await FailedCollection.CountDocumentsAsync(filter).ConfigureAwait(false);
            if (statuses.Any(_ => _ != JobStepStatus.Failed))
            {
                return (int)count;
            }
        }

        if (statuses.Length > 0)
        {
            filter &= Builders<JobStepState>.Filter.In(nameof(JobStepState.Status).ToCamelCase(), statuses);
        }
        count += await Collection.CountDocumentsAsync(filter).ConfigureAwait(false);
        return (int)count;
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<JobStepState>> ObserveForJob(JobId jobId)
    {
        var filter = GetJobIdFilter<ChangeStreamDocument<JobStepState>>(jobId, "fullDocument.");
        var initialItems = GetForJob(jobId).GetAwaiter().GetResult();

        return Collection.Observe(initialItems, HandleChangesForJobSteps, filter);
    }

    /// <summary>
    /// Get the id filter for a given <see cref="JobId"/> and <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="jobId">Identifier of the job.</param>
    /// <param name="prefix">The prefix to prepend to the field to query for.</param>
    /// <typeparam name="TDocument">Type of document to query.</typeparam>
    /// <returns><see cref="FilterDefinition{T}"/> for the BsonDocument.</returns>
    protected FilterDefinition<TDocument> GetJobIdFilter<TDocument>(Guid jobId, string prefix = "") =>
        Builders<TDocument>.Filter.Eq(
            new StringFieldDefinition<TDocument, BsonBinaryData>($"{prefix}_id.jobId"),
            new BsonBinaryData(jobId, GuidRepresentation.Standard));

    /// <summary>
    /// Get the id filter for a given <see cref="JobId"/> and <see cref="JobStepId"/>.
    /// </summary>
    /// <param name="jobId">Identifier of the job.</param>
    /// <param name="jobStepId">Identifier of the job step.</param>
    /// <typeparam name="TDocument">Type of document to query.</typeparam>
    /// <returns><see cref="FilterDefinition{T}"/> for the BsonDocument.</returns>
    protected FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid jobId, Guid jobStepId) =>
        Builders<TDocument>.Filter.Eq(
            new StringFieldDefinition<TDocument, BsonDocument>("_id"),
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

    void HandleChangesForJobSteps(IChangeStreamCursor<ChangeStreamDocument<JobStepState>> cursor, List<JobStepState> jobs)
    {
    }
}

/// <summary>
/// Represents an implementation of <see cref="IJobStepStorage{TJobState}"/> for MongoDB.
/// </summary>
/// <typeparam name="TJobStepState">Type of <see cref="JobStepState"/> to work with.</typeparam>
public class JobStepStorage<TJobStepState> : JobStepStorage, IJobStepStorage<TJobStepState>
    where TJobStepState : JobStepState
{
    readonly IEventStoreNamespaceDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStepStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="database">Provider for <see cref="IEventStoreNamespaceDatabase"/> for persistence.</param>
    public JobStepStorage(IEventStoreNamespaceDatabase database)
        : base(database)
    {
        _database = database;
    }

    IMongoCollection<TJobStepState> Collection => _database.GetCollection<TJobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<TJobStepState> FailedCollection => _database.GetCollection<TJobStepState>(WellKnownCollectionNames.FailedJobSteps);

    /// <inheritdoc/>
    public async Task<TJobStepState?> Read(JobId jobId, JobStepId jobStepId)
    {
        var filter = GetIdFilter<TJobStepState>(jobId, jobStepId);
        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState state)
    {
        var collection = state.Status == JobStepStatus.Failed ? FailedCollection : Collection;
        await collection.ReplaceOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId), state, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveToFailed(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState jobStepState)
    {
        await Collection.DeleteOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId)).ConfigureAwait(false);
        await Save(jobId, jobStepId, jobStepState);
    }
}
