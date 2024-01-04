// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Jobs;
using Aksio.Cratis.Kernel.MongoDB.Observation;
using Aksio.Cratis.Kernel.Storage.Jobs;
using Aksio.DependencyInversion;
using Aksio.Strings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Aksio.Cratis.Kernel.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
public class JobStorage : IJobStorage
{
    readonly ProviderFor<IEventStoreInstanceDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/> for persistence.</param>
    public JobStorage(ProviderFor<IEventStoreInstanceDatabase> databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<JobState> Collection => _databaseProvider().GetCollection<JobState>(WellKnownCollectionNames.Jobs);

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

    /// <summary>
    /// Get the filter for a specific <see cref="JobId"/>.
    /// </summary>
    /// <param name="id"><see cref="JobId"/> to get filter for.</param>
    /// <typeparam name="TDocument">Type of document.</typeparam>
    /// <returns>The <see cref="FilterDefinition{T}"/> to be used with a query.</returns>
    protected FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid id) => Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, Guid>("_id"), id);

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

/// <summary>
/// Represents an implementation of <see cref="IJobStorage{TJobState}"/> for MongoDB.
/// </summary>
/// <typeparam name="TJobState">Type of <see cref="JobState"/> to work with.</typeparam>
public class JobStorage<TJobState> : JobStorage, IJobStorage<TJobState>
    where TJobState : JobState
{
    readonly ProviderFor<IEventStoreInstanceDatabase> _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="JobStorage{TJobState}"/> class.
    /// </summary>
    /// <param name="databaseProvider">Provider for <see cref="IEventStoreInstanceDatabase"/> for persistence.</param>
    public JobStorage(ProviderFor<IEventStoreInstanceDatabase> databaseProvider) : base(databaseProvider)
    {
        _databaseProvider = databaseProvider;
    }

    IMongoCollection<TJobState> Collection => _databaseProvider().GetCollection<TJobState>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<TJobState?> Read(JobId jobId)
    {
        var cursor = await Collection.FindAsync(GetIdFilter<TJobState>(jobId)).ConfigureAwait(false);
        return cursor.FirstOrDefault();
    }

    /// <inheritdoc/>
    public async Task Save(JobId jobId, TJobState state) =>
        await Collection.ReplaceOneAsync(GetIdFilter<TJobState>(jobId), state, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IImmutableList<TJobState>> GetJobs<TJobType>(params JobStatus[] statuses)
    {
        var jobType = (JobType)typeof(TJobType);
        var jobTypeFilter = Builders<TJobState>.Filter.Eq(_ => _.Type, jobType);
        var statusFilters = statuses.Select(status => Builders<TJobState>.Filter.Eq(_ => _.Status, status));

        var filter = statuses.Length == 0 ?
                                jobTypeFilter :
                                Builders<TJobState>.Filter.And(jobTypeFilter, Builders<TJobState>.Filter.Or(statusFilters));

        var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
        var jobs = await cursor.ToListAsync().ConfigureAwait(false);
        return jobs.ToImmutableList();
    }
}
