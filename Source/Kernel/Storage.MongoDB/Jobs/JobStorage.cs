// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
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
    const string StatusElementName = "status";
    const string TypeElementName = "type";
    readonly ConcurrentDictionary<string, byte> _ensuredIndexes = new();

    /// <summary>
    /// Gets the indexes maintained on the jobs collection.
    /// </summary>
    /// <remarks>
    /// Keys are declared by stored BSON element name rather than by property expression: <see cref="JobState"/>
    /// is persisted through a custom, non-document serializer that the MongoDB LINQ provider cannot introspect,
    /// so an expression key over an enum member (Status/Type) throws ExpressionNotSupportedException when the
    /// index is rendered against the server's serializer registry.
    /// </remarks>
    internal static IReadOnlyList<CreateIndexModel<JobState>> Indexes { get; } =
    [
        new(
            Builders<JobState>.IndexKeys.Ascending(new StringFieldDefinition<JobState>(StatusElementName)),
            new CreateIndexOptions { Name = "status", Background = true }),
        new(
            Builders<JobState>.IndexKeys
                .Ascending(new StringFieldDefinition<JobState>(TypeElementName))
                .Ascending(new StringFieldDefinition<JobState>(StatusElementName)),
            new CreateIndexOptions { Name = "type_status", Background = true })
    ];

    IMongoCollection<JobState> Collection => database.GetCollection<JobState>(WellKnownCollectionNames.Jobs);

    /// <inheritdoc/>
    public async Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            using var cursor = await Collection.FindAsync(GetIdFilter<JobState>(jobId)).ConfigureAwait(false);
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
            return Catch.Success(Collection.Observe(StatusFilter<JobState>(statuses)));
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

            using var cursor = await GetTypedCollection<TJobState>().FindAsync(GetIdFilter<TJobState>(jobId)).ConfigureAwait(false);
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

            await EnsureIndexes().ConfigureAwait(false);
            using var cursor = await GetTypedCollection<TJobState>().FindAsync(TypeAndStatusFilter<TJobState>(jobType, statuses)).ConfigureAwait(false);
            var jobs = await cursor.ToListAsync().ConfigureAwait(false);
            return jobs.ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Gets the filter matching jobs in any of the given statuses, or every job when no status is given.
    /// </summary>
    /// <param name="statuses">The <see cref="JobStatus">statuses</see> to match.</param>
    /// <typeparam name="TDocument">The job state document type the filter is built for.</typeparam>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/> to query with.</returns>
    /// <remarks>
    /// Fields are named by stored BSON element name rather than by property expression, for the same reason the
    /// indexes are: <see cref="JobState"/> is persisted through a custom, non-document serializer the MongoDB LINQ
    /// provider cannot introspect, so an expression over an enum member throws ExpressionNotSupportedException when
    /// the filter is rendered against the server's serializer registry.
    /// </remarks>
    internal static FilterDefinition<TDocument> StatusFilter<TDocument>(IReadOnlyCollection<JobStatus> statuses) =>
        statuses.Count == 0
            ? Builders<TDocument>.Filter.Empty
            : Builders<TDocument>.Filter.Or(statuses.Select(status =>
                Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, JobStatus>(StatusElementName), status)));

    /// <summary>
    /// Gets the filter matching jobs of a given type, narrowed to any of the given statuses when statuses are given.
    /// </summary>
    /// <param name="jobType">The <see cref="JobType"/> to match.</param>
    /// <param name="statuses">The <see cref="JobStatus">statuses</see> to narrow to.</param>
    /// <typeparam name="TDocument">The job state document type the filter is built for.</typeparam>
    /// <returns>The <see cref="FilterDefinition{TDocument}"/> to query with.</returns>
    /// <remarks>
    /// Named by stored BSON element name for the reason given on <see cref="StatusFilter{TDocument}"/>.
    /// </remarks>
    internal static FilterDefinition<TDocument> TypeAndStatusFilter<TDocument>(JobType jobType, IReadOnlyCollection<JobStatus> statuses)
    {
        var jobTypeFilter = Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, JobType>(TypeElementName), jobType);
        return statuses.Count == 0
            ? jobTypeFilter
            : Builders<TDocument>.Filter.And(jobTypeFilter, StatusFilter<TDocument>(statuses));
    }

    static FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid id) => Builders<TDocument>.Filter.Eq(new StringFieldDefinition<TDocument, Guid>("_id"), id);

    IMongoCollection<TJobState> GetTypedCollection<TJobState>() => database.GetCollection<TJobState>(WellKnownCollectionNames.Jobs);

    async Task EnsureIndexes() => await Collection.EnsureIndexesOnceAsync(_ensuredIndexes, [.. Indexes]).ConfigureAwait(false);

    async Task<List<JobState>> GetJobsRaw(params JobStatus[] statuses)
    {
        await EnsureIndexes().ConfigureAwait(false);

        // The materialized Status field is kept in lockstep with the last status change (see the Job grain), so
        // filtering it directly is behavior-identical to the previous $arrayElemAt on statusChanges — and indexable.
        using var cursor = await Collection.FindAsync(StatusFilter<JobState>(statuses)).ConfigureAwait(false);
        return await cursor.ToListAsync().ConfigureAwait(false);
    }
}
