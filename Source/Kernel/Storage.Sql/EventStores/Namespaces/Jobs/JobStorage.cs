// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Subjects;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Jobs;

/// <summary>
/// Represents an implementation of <see cref="IJobStorage"/> using SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
/// <param name="jobTypes">The <see cref="IJobTypes"/> that knows about job types.</param>
public class JobStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database,
    IJobTypes jobTypes) : IJobStorage
{
    /// <inheritdoc/>
    public async Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            await using var scope = await database.Namespace(eventStore, @namespace);
            var job = await scope.DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
            return job is not null ? job.ToJobState() : JobError.NotFound;
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
            await using var scope = await database.Namespace(eventStore, @namespace);
            var query = scope.DbContext.Jobs.AsQueryable();

            if (statuses.Length > 0)
            {
                query = query.Where(j => statuses.Contains(j.Status));
            }

            var jobs = await query.ToListAsync();
            return jobs.Select(j => j.ToJobState()).ToImmutableList();
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
            // For SQL implementation, we'll create a simple subject that provides current data
            // This could be enhanced with actual database change tracking in the future
            var subject = new BehaviorSubject<IEnumerable<JobState>>([]);

            Task.Run(async () =>
            {
                var jobs = await GetJobs(statuses);
                if (jobs.TryPickT0(out var jobList, out _))
                {
                    subject.OnNext(jobList);
                }
            });

            return subject;
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
            await using var scope = await database.Namespace(eventStore, @namespace);
            var job = await scope.DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);
            if (job is not null)
            {
                scope.DbContext.Jobs.Remove(job);
                await scope.DbContext.SaveChangesAsync();
            }
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

            await using var scope = await database.Namespace(eventStore, @namespace);
            var job = await scope.DbContext.Jobs.FirstOrDefaultAsync(j => j.Id == jobId.Value);

            if (job is null)
            {
                return JobError.NotFound;
            }

            if (string.IsNullOrEmpty(job.StateJson))
            {
                return JobError.NotFound;
            }

            var jobState = JsonSerializer.Deserialize<TJobState>(job.StateJson);
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

            await using var scope = await database.Namespace(eventStore, @namespace);

            var job = new Job
            {
                Id = jobId.Value,
                StateJson = JsonSerializer.Serialize(state)
            };

            // Try to extract common properties from the job state if it inherits from JobState
            if (state is JobState jobState)
            {
                job.Type = jobState.Type.Value;
                job.Status = jobState.Status;
                job.Created = jobState.Created;
            }

            await scope.DbContext.Jobs.Upsert(job);
            await scope.DbContext.SaveChangesAsync();
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

            await using var scope = await database.Namespace(eventStore, @namespace);
            var query = scope.DbContext.Jobs.Where(j => j.Type == jobType);

            if (statuses.Length > 0)
            {
                query = query.Where(j => statuses.Contains(j.Status));
            }

            var jobs = await query.ToListAsync();
            var jobStates = new List<TJobState>();

            foreach (var job in jobs)
            {
                if (!string.IsNullOrEmpty(job.StateJson))
                {
                    var jobState = JsonSerializer.Deserialize<TJobState>(job.StateJson);
                    if (jobState is not null)
                    {
                        jobStates.Add(jobState);
                    }
                }
            }

            return jobStates.ToImmutableList();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
