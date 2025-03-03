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
    /// <summary>
    /// Wait for there to be jobs in the event store, with an optional timeout.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> to work with.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <returns>Collection of jobs.</returns>
    public static async Task<IEnumerable<Job>> WaitForThereToBeJobs(this IEventStore eventStore, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var jobs = Enumerable.Empty<Job>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!jobs.Any() && !cts.IsCancellationRequested)
        {
            jobs = await eventStore.GetJobs();
            await Task.Delay(20);
        }

        return jobs;
    }


    /// <summary>
    /// Wait for there to be no jobs in the event store, with an optional timeout.
    /// </summary>
    /// <param name="eventStore"><see cref="IEventStore"/> to work with.</param>
    /// <param name="timeout">Optional timeout. If none is provided, it will default to 5 seconds.</param>
    /// <param name="includeStatuses">The statuses a job can have.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<Job>> WaitForThereToBeNoJobs(this IEventStore eventStore, TimeSpan? timeout = default, params JobStatus[] includeStatuses)
    {
        timeout ??= TimeSpan.FromSeconds(5);
        var jobs = await eventStore.GetJobs();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (jobs.Any() && !jobs.All(_ => includeStatuses.Contains(_.Status)) && !cts.IsCancellationRequested)
        {
            jobs = await eventStore.GetJobs();
            await Task.Delay(20);
        }
        return jobs;
    }

    public static async Task<TJobState> WaitTillJobCompleted<TJobState>(this IJobStorage jobStorage, JobId jobId, TimeSpan? timeout = null)
        where TJobState : JobState
    {
        timeout ??= TimeSpan.FromSeconds(5);
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var getJobState = await jobStorage.Read<TJobState>(jobId);
            var conditionMet = getJobState.Match(state => state.Progress.IsCompleted, _ => false, _ => false);
            if (conditionMet)
            {
                return getJobState.AsT0;
            }
            await Task.Delay(20, cts.Token);
        }
    }

    static Task<IEnumerable<Job>> GetJobs(this IEventStore eventStore) =>
         eventStore.Connection.Services.Jobs.GetJobs(new()
         {
             EventStore = eventStore.Name.Value,
             Namespace = Concepts.EventStoreNamespaceName.Default
         });
}
