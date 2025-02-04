// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Strings;
using MongoDB.Bson;
using MongoDB.Driver;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.MongoDB.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> for MongoDB.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStorage"/> class.
/// </remarks>
/// <param name="database"><see cref="IEventStoreNamespaceDatabase"/> for persistence.</param>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about <see cref="JobType"/>.</param>
public class JobStorage(IEventStoreNamespaceDatabase database, IJobTypes jobTypes) : IJobStorage
{
    IMongoCollection<JobState> Collection => database.GetCollection<JobState>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            var cursor = await Collection.FindAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);
            var job = await cursor.SingleOrDefaultAsync();
#pragma warning disable RCS1084 // This is more clear
            return job is not null ? job : JobError.NotFound;
#pragma warning restore RCS1084
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses)
    {
        try
        {
            var jobs = await GetJobsRaw(statuses).ConfigureAwait(false);
            return jobs.ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses)
    {
        try
        {
            var statusFilters = statuses.Select(status =>
                Builders<JobState>.Filter.Eq(_ => _.Status, status));

            var filter = statuses.Length == 0 ?
                Builders<JobState>.Filter.Empty :
                Builders<JobState>.Filter.Or(statusFilters);

            return Catch.Success(Collection.Observe(filter));
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> Remove(JobId jobId)
    {
        try
        {
            await Collection.DeleteOneAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }

            var cursor = await GetTypedCollection<TJobState>().FindAsync(GetIdFilter<TJobState>(jobId)).ConfigureAwait(false);
            var jobState = await cursor.FirstOrDefaultAsync();
            return jobState is not null ? jobState : JobError.NotFound;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }

            await GetTypedCollection<TJobState>()
                .ReplaceOneAsync(GetIdFilter<TJobState>(jobId), state, new ReplaceOptions { IsUpsert = true })
                .ConfigureAwait(false);

            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return error;
            }
            if (jobTypes.GetFor(typeof(TJobType)).TryPickT1(out _, out var jobType))
            {
                return JobError.TypeIsNotAssociatedWithAJobType;
            }

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
        catch (Exception ex)
        {
            return ex;
        }
    }

    static FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid id) => Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, Guid>("_id"), id);

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