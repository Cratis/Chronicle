// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for persistence.</param>
public class JobStepStorage(IEventStoreNamespaceDatabase database) : IJobStepStorage
{
    IMongoCollection<JobStepState> Collection => database.GetCollection<JobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<JobStepState> FailedCollection => database.GetCollection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);

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
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public async Task<TJobStepState?> Read<TJobStepState>(JobId jobId, JobStepId jobStepId)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobStepState));
        var filter = GetIdFilter<TJobStepState>(jobId, jobStepId);
        var cursor = await GetTypedCollection<TJobStepState>().FindAsync(filter).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save<TJobStepState>(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState state)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobStepState));
        var actualState = (state as JobStepState)!;
        var collection = actualState.Status == JobStepStatus.Failed ? GetTypedFailedCollection<TJobStepState>() : GetTypedCollection<TJobStepState>();
        await collection.ReplaceOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId), state, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task MoveToFailed<TJobStepState>(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState jobStepState)
    {
        InvalidJobStepStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobStepState));
        await Save(jobId, jobStepId, jobStepState);
        await GetTypedCollection<TJobStepState>().DeleteOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId)).ConfigureAwait(false);
    }

    IMongoCollection<TJobStepState> GetTypedCollection<TJobStepState>() => database.GetCollection<TJobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<TJobStepState> GetTypedFailedCollection<TJobStepState>() => database.GetCollection<TJobStepState>(WellKnownCollectionNames.FailedJobSteps);

    FilterDefinition<TDocument> GetJobIdFilter<TDocument>(Guid jobId, string prefix = "") =>
        Builders<TDocument>.Filter.Eq(
            new StringFieldDefinition<TDocument, BsonBinaryData>($"{prefix}_id.jobId"),
            new BsonBinaryData(jobId, GuidRepresentation.Standard));

    FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid jobId, Guid jobStepId) =>
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
}
