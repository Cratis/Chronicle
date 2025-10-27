// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.JobSteps;

/// <summary>
/// Represents an implementation of <see cref="IJobStepStorage"/> using SQL.
/// </summary>
/// <param name="eventStore">The name of the event store.</param>
/// <param name="namespace">The name of the namespace.</param>
/// <param name="database">The <see cref="IDatabase"/> to use for storage operations.</param>
public class JobStepStorage(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IDatabase database) : IJobStepStorage
{
    /// <inheritdoc/>
    public async Task<Catch> RemoveAllForJob(JobId jobId)
    {
        try
        {
            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobSteps = scope.DbContext.JobSteps.Where(js => js.JobId == jobId.Value);
            scope.DbContext.JobSteps.RemoveRange(jobSteps);
            await scope.DbContext.SaveChangesAsync();
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
            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobSteps = scope.DbContext.JobSteps.Where(js => js.JobId == jobId.Value && js.Status != JobStepStatus.Failed);
            scope.DbContext.JobSteps.RemoveRange(jobSteps);
            await scope.DbContext.SaveChangesAsync();
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
            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobStep = await scope.DbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);
            if (jobStep is not null)
            {
                scope.DbContext.JobSteps.Remove(jobStep);
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
    public async Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            await using var scope = await database.Namespace(eventStore, @namespace);
            var query = scope.DbContext.JobSteps.Where(js => js.JobId == jobId.Value);

            if (statuses.Length > 0)
            {
                query = query.Where(js => statuses.Contains(js.Status));
            }

            var jobSteps = await query.ToListAsync();
            return jobSteps.Select(js => js.ToJobStepState()).ToImmutableList();
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
            await using var scope = await database.Namespace(eventStore, @namespace);
            var query = scope.DbContext.JobSteps.Where(js => js.JobId == jobId.Value);

            if (statuses.Length > 0)
            {
                query = query.Where(js => statuses.Contains(js.Status));
            }

            return await query.CountAsync();
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
            // For SQL implementation, we'll create a simple observable that provides current data
            // This could be enhanced with actual database change tracking in the future
            var observable = Observable.Create<IEnumerable<JobStepState>>(async observer =>
            {
                var jobSteps = await GetForJob(jobId);
                if (jobSteps.TryPickT0(out var jobStepList, out _))
                {
                    observer.OnNext(jobStepList);
                }
                observer.OnCompleted();
                return Disposable.Empty;
            });
            return (Catch<IObservable<IEnumerable<JobStepState>>>)observable;
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

            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobStep = await scope.DbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);

            if (jobStep is null)
            {
                return JobStepError.NotFound;
            }

            if (string.IsNullOrEmpty(jobStep.StateJson))
            {
                return JobStepError.NotFound;
            }

            var jobStepState = JsonSerializer.Deserialize<TJobStepState>(jobStep.StateJson);
            return jobStepState is not null ? jobStepState : JobStepError.NotFound;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobStep = await scope.DbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);

            if (jobStep is null)
            {
                // Create new job step entry
                var entityId = Guid.NewGuid();

                if (state is JobStepState jobStepState)
                {
                    jobStep = jobStepState.ToEntity(entityId);
                }
                else
                {
                    jobStep = new JobStep
                    {
                        Id = entityId,
                        JobId = jobId.Value,
                        JobStepId = jobStepId.Value,
                        StateJson = JsonSerializer.Serialize(state)
                    };
                }

                scope.DbContext.JobSteps.Add(jobStep);
            }
            else
            {
                // Update existing job step
                jobStep.StateJson = JsonSerializer.Serialize(state);

                // Update common properties if the state is a JobStepState
                if (state is JobStepState jobStepState)
                {
                    jobStep.Type = jobStepState.Type.Value;
                    jobStep.Name = jobStepState.Name.Value;
                    jobStep.Status = jobStepState.Status;
                    jobStep.IsPrepared = jobStepState.IsPrepared;
                }
            }

            await scope.DbContext.SaveChangesAsync();
            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public async Task<Catch<None, JobStepError>> MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return error;
            }

            await using var scope = await database.Namespace(eventStore, @namespace);
            var jobStep = await scope.DbContext.JobSteps.FirstOrDefaultAsync(js => js.JobId == jobId.Value && js.JobStepId == jobStepId.Value);

            if (jobStep is not null)
            {
                // Update status to failed and save the state
                jobStep.Status = JobStepStatus.Failed;
                jobStep.StateJson = JsonSerializer.Serialize(jobStepState);

                if (jobStepState is JobStepState stepState)
                {
                    jobStep.Type = stepState.Type.Value;
                    jobStep.Name = stepState.Name.Value;
                    jobStep.IsPrepared = stepState.IsPrepared;
                }

                await scope.DbContext.SaveChangesAsync();
            }

            return default(None);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
