// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
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
public class JobStorage(IEventStoreNamespaceDatabase database) : IJobStorage
{
    IMongoCollection<JobState> Collection => database.GetCollection<JobState>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<JobState> GetJob(JobId jobId)
    {
        var cursor = await Collection.FindAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);
        return cursor.Single();
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<JobState>> GetJobs(params JobStatus[] statuses)
    {
        var jobs = await GetJobsRaw(statuses).ConfigureAwait(false);
        return jobs.ToImmutableList();
    }

    /// <inheritdoc/>
    public ISubject<IEnumerable<JobState>> ObserveJobs(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status =>
            Builders<JobState>.Filter.Eq(_ => _.Status, status));

        var filter = statuses.Length == 0 ?
                                Builders<JobState>.Filter.Empty :
                                Builders<JobState>.Filter.Or(statusFilters);

        return Collection.Observe(filter);
    }

    /// <inheritdoc/>
    public async Task Remove(JobId jobId) =>
        await Collection.DeleteOneAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<TJobState?> Read<TJobState>(JobId jobId)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobState));
        var cursor = await GetTypedCollection<TJobState>().FindAsync(GetIdFilter<TJobState>(jobId)).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save<TJobState>(JobId jobId, TJobState state)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobState));
        await GetTypedCollection<TJobState>().ReplaceOneAsync(GetIdFilter<TJobState>(jobId), state, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IImmutableList<TJobState>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses)
    {
        InvalidJobStateType.ThrowIfTypeDoesNotDeriveFromJobState(typeof(TJobState));
        var jobType = (JobType)typeof(TJobType);
        var jobTypeFilter = Builders<JobState>.Filter.Eq(_ => _.Type, jobType);
        var statusFilters = statuses.Select(status => Builders<JobState>.Filter.Eq(_ => _.Status, status));

        var filter = statuses.Length == 0 ?
                                jobTypeFilter :
                                Builders<JobState>.Filter.And(jobTypeFilter, Builders<JobState>.Filter.Or(statusFilters));
        var filterAsBsonDocument = filter.ToBsonDocument();
        var cursor = await GetTypedCollection<TJobState>().FindAsync(filterAsBsonDocument).ConfigureAwait(false);
        var jobs = await cursor.ToListAsync().ConfigureAwait(false);
        return jobs.ToImmutableList();
    }

    FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid id) => Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, Guid>("_id"), id);

    IMongoCollection<TJobState> GetTypedCollection<TJobState>() => database.GetCollection<TJobState>(WellKnownCollectionNames.Jobs);

    async Task<List<JobState>> GetJobsRaw(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status => Builders<BsonDocument>.Filter.Eq(new StringFieldDefinition<BsonDocument, JobStatus>(nameof(JobState.Status).ToCamelCase()), status));

        if (statuses.Length == 0)
        {
            var cursor = await Collection.FindAsync(_ => true).ConfigureAwait(false);
            return await cursor.ToListAsync().ConfigureAwait(false);
        }

        var filter = new BsonDocument
        {
            {
                "$expr", new BsonDocument("$in", new BsonArray
                {
                    new BsonDocument(
                        "$arrayElemAt",
                        new BsonArray
                        {
                                "$statusChanges.status",
                                -1
                        }),
                    new BsonArray(statuses)
                })
            }
        };

        var aggregation = Collection.Aggregate().Match(filter);
        return await aggregation.ToListAsync().ConfigureAwait(false);
    }
}
