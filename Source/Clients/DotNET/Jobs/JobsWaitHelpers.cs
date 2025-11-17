// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Helper methods for working with jobs for integration testing purposes.
/// </summary>
/// <remarks>
/// These are useful for integration tests where you want to ensure that a job has reached a certain state.
/// </remarks>>
public static class JobsWaitHelpers
{
    const int DefaultDelay = 50;

    /// <summary>
    /// Waits for there to be jobs in the system.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<Job>> WaitForThereToBeJobs(this IJobs jobs, TimeSpan? timeout = default)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var currentJobs = Enumerable.Empty<Job>();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (!currentJobs.Any() && !cts.IsCancellationRequested)
        {
            currentJobs = await jobs.GetJobs();
            await Task.Delay(DefaultDelay);
        }

        return currentJobs;
    }

    /// <summary>
    /// Waits for there to be no jobs in the system.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <param name="includeStatuses">Optional job statuses to include in the check. Defaults to all statuses.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<IEnumerable<Job>> WaitForThereToBeNoJobs(this IJobs jobs, TimeSpan? timeout = default, params JobStatus[] includeStatuses)
    {
        var statuses = new List<JobStatus>(includeStatuses);
        timeout ??= TimeSpanFactory.DefaultTimeout();
        var currentJobs = await jobs.GetJobs();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (currentJobs.Any() && !currentJobs.All(_ => includeStatuses.Contains(_.Status)) && !cts.IsCancellationRequested)
        {
            currentJobs = await jobs.GetJobs();
            await Task.Delay(DefaultDelay);
        }
        return currentJobs;
    }

    /// <summary>
    /// Waits for a job to be completed.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="jobId">Job ID.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static Task<Job> WaitTillJobProgressCompleted(this IJobs jobs, JobId jobId, TimeSpan? timeout = null) =>
        jobs.WaitTillJobMeetsPredicate(jobId, state => state.Progress.IsCompleted, timeout);

    /// <summary>
    /// Waits for a job to be stopped.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="jobId">Job ID.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static Task<Job> WaitTillJobProgressStopped(this IJobs jobs, JobId jobId, TimeSpan? timeout = null) =>
        jobs.WaitTillJobMeetsPredicate(jobId, state => state.Progress.IsStopped, timeout);

    /// <summary>
    /// Waits for a job to be deleted.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="jobId">Job ID.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WaitTillJobIsDeleted(this IJobs jobs, JobId jobId, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var currentJobs = await jobs.GetJobs();
            if (!currentJobs.Any(_ => _.Id == jobId))
            {
                return;
            }
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }

    /// <summary>
    /// Waits for a job to reach a certain state based on a predicate.
    /// </summary>
    /// <param name="jobs"><see cref="IJobs"/> system.</param>
    /// <param name="jobId">Job ID.</param>
    /// <param name="predicate">Predicate to check the job state.</param>
    /// <param name="timeout">Optional timeout. Defaults to 5 seconds.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task<Job> WaitTillJobMeetsPredicate(this IJobs jobs, JobId jobId, Func<Job, bool> predicate, TimeSpan? timeout = null)
    {
        timeout ??= TimeSpanFactory.DefaultTimeout();
        using var cts = new CancellationTokenSource(timeout.Value);
        while (true)
        {
            var currentJob = await jobs.GetJob(jobId);
            if (currentJob is not null && predicate(currentJob))
            {
                return currentJob;
            }
            await Task.Delay(DefaultDelay, cts.Token);
        }
    }
}
