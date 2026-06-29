// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.InMemory.Jobs;

/// <summary>
/// Represents an in-memory implementation of <see cref="IJobStepStorage"/>.
/// </summary>
public sealed class JobStepStorage : IJobStepStorage
{
    readonly ConcurrentDictionary<JobStepIdentifier, JobStepState> _steps = new();
    readonly ConcurrentDictionary<JobStepIdentifier, JobStepState> _failedSteps = new();

    /// <inheritdoc/>
    public Task<Catch> RemoveAllForJob(JobId jobId)
    {
        try
        {
            RemoveAll(_steps, jobId);
            RemoveAll(_failedSteps, jobId);
            return Task.FromResult(Catch.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch> RemoveAllNonFailedForJob(JobId jobId)
    {
        try
        {
            RemoveAll(_steps, jobId);
            return Task.FromResult(Catch.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch> Remove(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            var key = new JobStepIdentifier(jobId, jobStepId);
            _steps.TryRemove(key, out _);
            _failedSteps.TryRemove(key, out _);
            return Task.FromResult(Catch.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<JobStepState>>> GetForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            var steps = ForJob(_steps, jobId, statuses)
                .Concat(ForJob(_failedSteps, jobId, statuses))
                .ToImmutableList();

            return Task.FromResult<Catch<IImmutableList<JobStepState>>>(steps);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<JobStepState>>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<int>> CountForJob(JobId jobId, params JobStepStatus[] statuses)
    {
        try
        {
            var count = ForJob(_steps, jobId, statuses).Count() + ForJob(_failedSteps, jobId, statuses).Count();
            return Task.FromResult<Catch<int>>(count);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<int>>(ex);
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
    public Task<Catch<TJobStepState, JobStepError>> Read<TJobStepState>(JobId jobId, JobStepId jobStepId)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<TJobStepState, JobStepError>>(error);
            }

            var key = new JobStepIdentifier(jobId, jobStepId);
            if (_steps.TryGetValue(key, out var step) && step is TJobStepState typed)
            {
                return Task.FromResult<Catch<TJobStepState, JobStepError>>(typed);
            }

            if (_failedSteps.TryGetValue(key, out var failedStep) && failedStep is TJobStepState failedTyped)
            {
                return Task.FromResult<Catch<TJobStepState, JobStepError>>(failedTyped);
            }

            return Task.FromResult<Catch<TJobStepState, JobStepError>>(JobStepError.NotFound);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<TJobStepState, JobStepError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<None, JobStepError>> Save<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState state)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<None, JobStepError>>(error);
            }

            var key = new JobStepIdentifier(jobId, jobStepId);
            var actualState = (state as JobStepState)!;
            if (ShouldBeStoredInFailedCollection(actualState.Status))
            {
                _failedSteps[key] = actualState;
            }
            else
            {
                _steps[key] = actualState;
            }

            return Task.FromResult<Catch<None, JobStepError>>(default(None));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<None, JobStepError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<None, JobStepError>> MoveToFailed<TJobStepState>(JobId jobId, JobStepId jobStepId, TJobStepState jobStepState)
    {
        try
        {
            if (JobStepStateType.Verify(typeof(TJobStepState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<None, JobStepError>>(error);
            }

            var key = new JobStepIdentifier(jobId, jobStepId);
            _failedSteps[key] = (jobStepState as JobStepState)!;
            _steps.TryRemove(key, out _);
            return Task.FromResult<Catch<None, JobStepError>>(default(None));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<None, JobStepError>>(ex);
        }
    }

    static bool ShouldBeStoredInFailedCollection(JobStepStatus status) => status is JobStepStatus.Failed or JobStepStatus.CompletedWithFailure;

    static void RemoveAll(ConcurrentDictionary<JobStepIdentifier, JobStepState> collection, JobId jobId)
    {
        foreach (var key in collection.Keys.Where(_ => _.JobId == jobId).ToArray())
        {
            collection.TryRemove(key, out _);
        }
    }

    static IEnumerable<JobStepState> ForJob(ConcurrentDictionary<JobStepIdentifier, JobStepState> collection, JobId jobId, JobStepStatus[] statuses)
    {
        var steps = collection.Where(_ => _.Key.JobId == jobId).Select(_ => _.Value);
        return statuses.Length == 0 ? steps : steps.Where(_ => statuses.Contains(_.Status));
    }
}
