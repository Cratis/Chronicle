// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
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
public class JobStepStorage(IEventStoreNamespaceDatabase database) : IJobStepStorage
{
    IMongoCollection<JobStepState> Collection => database.GetCollection<JobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<JobStepState> FailedCollection => database.GetCollection<JobStepState>(WellKnownCollectionNames.FailedJobSteps);

    /// <inheritdoc/>
    public async Task<Catch> RemoveAllForJob(JobId jobId)
    {
        try
        {
            await Collection.DeleteManyAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
            await FailedCollection.DeleteManyAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> RemoveAllNonFailedForJob(JobId jobId)
    {
        try
        {
            await Collection.DeleteManyAsync(GetJobIdFilter<JobStepState>(jobId)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch> Remove(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            await Collection.DeleteOneAsync(GetIdFilter<JobStepState>(jobId, jobStepId)).ConfigureAwait(false);
            await FailedCollection.DeleteOneAsync(GetIdFilter<JobStepState>(jobId, jobStepId)).ConfigureAwait(false);
            return Catch.Success();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            var includeAll = ShouldIncludeAllJobSteps(statuses);
            var failedJobSteps = new List<JobStepState>();
            var jobSteps = new List<JobStepState>();
            var filter = GetJobIdFilter<JobStepState>(jobId);

            if (statuses.Length > 0)
            {
                filter &= Builders<JobStepState>.Filter.In(nameof(JobStepState.Status).ToCamelCase(), statuses);
            }

            if (ShouldBeStoredInFailedCollection(statuses) || includeAll)
            {
                var failedCursor = await FailedCollection.FindAsync(filter).ConfigureAwait(false);
                failedJobSteps = failedCursor.ToList();
            }
            if (!statuses.All(ShouldBeStoredInFailedCollection) || includeAll)
            {
                var cursor = await Collection.FindAsync(filter).ConfigureAwait(false);
                jobSteps = cursor.ToList();
            }

            return jobSteps.Concat(failedJobSteps).ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<int>> CountForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            var includeAll = ShouldIncludeAllJobSteps(statuses);
            var filter = GetJobIdFilter<JobStepState>(jobId);
            var count = 0L;

            if (statuses.Length > 0)
            {
                filter &= Builders<JobStepState>.Filter.In(nameof(JobStepState.Status).ToCamelCase(), statuses);
            }

            if (ShouldBeStoredInFailedCollection(statuses) || includeAll)
            {
                count = await FailedCollection.CountDocumentsAsync(filter).ConfigureAwait(false);
            }
            if (!statuses.All(ShouldBeStoredInFailedCollection) || includeAll)
            {
                count += await Collection.CountDocumentsAsync(filter).ConfigureAwait(false);
            }
            return (int)count;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Catch<IObservable<IEnumerable<JobStepState>>> ObserveForJob(JobId jobId)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<TJobStepState, JobStepError>> Read<TJobStepState>(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            var filter = GetIdFilter<TJobStepState>(jobId, jobStepId);
            var cursor = await GetTypedCollection<TJobStepState>().FindAsync(filter).ConfigureAwait(false);
            var state = await cursor.FirstOrDefaultAsync();
            return state is not null ? state : JobStepError.NotFound;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> Save<TJobStepState>(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState state)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            var actualState = (state as JobStepState)!;
            var collection = ShouldBeStoredInFailedCollection(actualState.Status)
                ? GetTypedFailedCollection<TJobStepState>()
                : GetTypedCollection<TJobStepState>();
            await collection.ReplaceOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId), state, new ReplaceOptions { IsUpsert = true }).ConfigureAwait(false);
            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> MoveToFailed<TJobStepState>(
        JobId jobId,
        JobStepId jobStepId,
        TJobStepState jobStepState)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            var saveResult = await Save(jobId, jobStepId, jobStepState);
            if (!saveResult.IsSuccess)
            {
                return saveResult;
            }

            await GetTypedCollection<TJobStepState>().DeleteOneAsync(GetIdFilter<TJobStepState>(jobId, jobStepId)).ConfigureAwait(false);
            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    static FilterDefinition<TDocument> GetJobIdFilter<TDocument>(Guid jobId, string prefix = "") =>
        Builders<TDocument>.Filter.Eq(
            new StringFieldDefinition<TDocument, BsonBinaryData>($"{prefix}_id.jobId"),
            new(jobId, GuidRepresentation.Standard));

    static FilterDefinition<TDocument> GetIdFilter<TDocument>(Guid jobId, Guid jobStepId) =>
        Builders<TDocument>.Filter.Eq(
            new StringFieldDefinition<TDocument, BsonDocument>("_id"),
            new()
            {
                {
                    "jobId",
                    new BsonBinaryData(jobId, GuidRepresentation.Standard)
                },
                {
                    "jobStepId", new BsonBinaryData(jobStepId, GuidRepresentation.Standard)
                }
            });

    static bool ShouldBeStoredInFailedCollection(JobStepStatus[] statuses) => statuses.Any(status => status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure);

    static bool ShouldBeStoredInFailedCollection(JobStepStatus status) => status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure;

    static bool ShouldIncludeAllJobSteps(JobStepStatus[] statuses) => statuses.Length == 0;

    IMongoCollection<TJobStepState> GetTypedCollection<TJobStepState>() => database.GetCollection<TJobStepState>(WellKnownCollectionNames.JobSteps);

    IMongoCollection<TJobStepState> GetTypedFailedCollection<TJobStepState>() => database.GetCollection<TJobStepState>(WellKnownCollectionNames.FailedJobSteps);
}
