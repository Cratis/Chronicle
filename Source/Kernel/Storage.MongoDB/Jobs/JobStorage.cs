// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Jobs;
using Cratis.Kernel.Storage.Jobs;
using Cratis.Kernel.Storage.MongoDB.Observation;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Kernel.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
public class JobStorage : IJobStorage
{
    readonly IEventStoreNamespaceDatabase _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage"/> class.
    /// </summary>
    /// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for persistence.</param>
    public JobStorage(IEventStoreNamespaceDatabase database)
    {
        _database = database;
    }

    IMongoCollection<JobState> Collection => _database.GetCollection<JobState>(WellKnownCollectionNames.Jobs);

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
    public IObservable<IEnumerable<JobState>> ObserveJobs(params JobStatus[] statuses)
    {
        var statusFilters = statuses.Select(status =>
            Builders<ChangeStreamDocument<JobState>>.Filter.Eq(_ => _.FullDocument.Status, status));

        var initialItems = GetJobs(statuses).GetAwaiter().GetResult();

        var filter = statuses.Length == 0 ?
                                Builders<ChangeStreamDocument<JobState>>.Filter.Empty :
                                Builders<ChangeStreamDocument<JobState>>.Filter.Or(statusFilters);

        return Collection.Observe(initialItems, HandleChangesForJobs, filter);
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

    IMongoCollection<TJobState> GetTypedCollection<TJobState>() => _database.GetCollection<TJobState>(WellKnownCollectionNames.Jobs);

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

    void HandleChangesForJobs(IChangeStreamCursor<ChangeStreamDocument<JobState>> cursor, List<JobState> jobs)
    {
        foreach (var change in cursor.Current)
        {
            var jobState = change.FullDocument;
            if (change.OperationType == ChangeStreamOperationType.Delete)
            {
                var job = jobs.Find(_ => _.Id == (JobId)change.DocumentKey["_id"].AsGuid);
                if (job is not null)
                {
                    jobs.Remove(job);
                }
                continue;
            }

            var observer = jobs.Find(_ => _.Id == jobState.Id);
            if (observer is not null)
            {
                var index = jobs.IndexOf(observer);
                jobs[index] = jobState;
            }
            else
            {
                jobs.Add(jobState);
            }
        }
    }
}
