// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using OneOf.Types;

namespace Cratis.Chronicle.Storage.InMemory.Jobs;

/// <summary>
/// Represents an in-memory implementation of <see cref="IJobStorage"/>.
/// </summary>
/// <param name="jobTypes"><see cref="IJobTypes"/> that knows about <see cref="JobType"/>.</param>
public sealed class JobStorage(IJobTypes jobTypes) : IJobStorage, IDisposable
{
    readonly ConcurrentDictionary<JobId, JobState> _jobs = new();
    readonly ReplaySubject<IEnumerable<JobState>> _subject = new(1);

    /// <inheritdoc/>
    public Task<Catch<JobState, JobError>> GetJob(JobId jobId)
    {
        try
        {
            return Task.FromResult<Catch<JobState, JobError>>(
                _jobs.TryGetValue(jobId, out var job) ? job : JobError.NotFound);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<JobState, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<JobState>>> GetJobs(params JobStatus[] statuses)
    {
        try
        {
            var jobs = FilterByStatus(_jobs.Values, statuses).ToImmutableList();
            return Task.FromResult<Catch<IImmutableList<JobState>>>(jobs);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<JobState>>>(ex);
        }
    }

    /// <inheritdoc/>
    public Catch<ISubject<IEnumerable<JobState>>> ObserveJobs(params JobStatus[] statuses)
    {
        try
        {
            PublishSnapshot();
            return Catch.Success<ISubject<IEnumerable<JobState>>>(_subject);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <inheritdoc/>
    public Task<Catch> Remove(JobId jobId)
    {
        try
        {
            _jobs.TryRemove(jobId, out _);
            PublishSnapshot();
            return Task.FromResult(Catch.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<TJobState, JobError>> Read<TJobState>(JobId jobId)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<TJobState, JobError>>(error);
            }

            if (_jobs.TryGetValue(jobId, out var job) && job is TJobState typed)
            {
                return Task.FromResult<Catch<TJobState, JobError>>(typed);
            }

            return Task.FromResult<Catch<TJobState, JobError>>(JobError.NotFound);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<TJobState, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<None, JobError>> Save<TJobState>(JobId jobId, TJobState state)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<None, JobError>>(error);
            }

            _jobs[jobId] = (state as JobState)!;
            PublishSnapshot();
            return Task.FromResult<Catch<None, JobError>>(default(None));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<None, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public Task<Catch<IImmutableList<TJobState>, JobError>> GetJobs<TJobType, TJobState>(params JobStatus[] statuses)
    {
        try
        {
            if (JobStateType.Verify(typeof(TJobState)).TryGetError(out var error))
            {
                return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(error);
            }

            if (jobTypes.GetFor(typeof(TJobType)).TryPickT1(out _, out var jobType))
            {
                return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(JobError.TypeIsNotAssociatedWithAJobType);
            }

            var jobs = FilterByStatus(_jobs.Values.Where(_ => _.Type == jobType), statuses)
                .OfType<TJobState>()
                .ToImmutableList();

            return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(jobs);
        }
        catch (Exception ex)
        {
            return Task.FromResult<Catch<IImmutableList<TJobState>, JobError>>(ex);
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _subject.Dispose();

    static IEnumerable<JobState> FilterByStatus(IEnumerable<JobState> jobs, JobStatus[] statuses) =>
        statuses.Length == 0 ? jobs : jobs.Where(_ => statuses.Contains(_.Status));

    void PublishSnapshot() => _subject.OnNext([.. _jobs.Values]);
}
