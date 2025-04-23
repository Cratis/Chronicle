// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Jobs;
using JobStatus = Cratis.Chronicle.Contracts.Jobs.JobStatus;

namespace Cratis.Chronicle.Integration.Orleans.InProcess;

/// <summary>
/// Helper methods for working with jobs for integration testing purposes.
/// </summary>
public static class JobsHelpers
{
    public static async Task<IEnumerable<Job>> WaitForThereToBeJobs(this IEventStore eventStore, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var jobs = Enumerable.Empty<Job>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!jobs.Any() && !cts.IsCancellationRequested)
        {
            jobs = await eventStore.GetJobs();
            await Task.Delay(20);
        }

        return jobs;
    }

    public static async Task<IEnumerable<Job>> WaitForThereToBeNoJobs(this IEventStore eventStore, TimeSpan? timeout = default, params JobStatus[] includeStatuses)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var jobs = await eventStore.GetJobs();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (jobs.Any() && !jobs.All(_ => includeStatuses.Contains(_.Status)) && !cts.IsCancellationRequested)
        {
            jobs = await eventStore.GetJobs();
            await Task.Delay(20);
        }
        return jobs;
    }

    public static Task<TJobState> WaitTillJobProgressCompleted<TJobState>(this IJobStorage jobStorage, JobId jobId, TimeSpan? timeout = null)
        where TJobState : JobState
        => WaitTillJobMeetsPredicate<TJobState>(jobStorage, jobId, state => state.Progress.IsCompleted, timeout);

    public static Task<TJobState> WaitTillJobProgressStopped<TJobState>(this IJobStorage jobStorage, JobId jobId, TimeSpan? timeout = null)
        where TJobState : JobState
        => WaitTillJobMeetsPredicate<TJobState>(jobStorage, jobId, state => state.Progress.IsStopped, timeout);

    public static async Task WaitTillJobIsDeleted<TJobState>(this IJobStorage jobStorage, JobId jobId, TimeSpan? timeout = null)
        where TJobState : JobState
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var getJobState = await jobStorage.Read<TJobState>(jobId);
            if (getJobState.TryGetError(out _))
            {
                return;
            }
            await Task.Delay(100, cts.Token);
        }
    }
    public static async Task<TJobState> WaitTillJobMeetsPredicate<TJobState>(this IJobStorage jobStorage, JobId jobId, Func<JobState, bool> predicate, TimeSpan? timeout = null)
        where TJobState : JobState
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var getJobState = await jobStorage.Read<TJobState>(jobId);
            var conditionMet = getJobState.Match(predicate, _ => false, _ => false);
            if (conditionMet)
            {
                return getJobState.AsT0;
            }
            await Task.Delay(100, cts.Token);
        }
    }

    static Task<IEnumerable<Job>> GetJobs(this IEventStore eventStore) =>
         eventStore.Connection.Services.Jobs.GetJobs(new()
         {
             EventStore = eventStore.Name.Value,
             Namespace = Concepts.EventStoreNamespaceName.Default
         });
}
