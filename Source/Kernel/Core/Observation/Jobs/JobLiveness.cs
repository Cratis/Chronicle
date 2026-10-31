// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Judges whether a job recorded as running still is.
/// </summary>
internal static class JobLiveness
{
    /// <summary>
    /// How long a job whose steps have all concluded may stay recorded as running before it is considered lost.
    /// </summary>
    /// <remarks>
    /// Finishing a job - reporting the replay or catch-up to the observer - follows the last step directly, so this is
    /// well beyond the window in which a healthy job is legitimately finalizing.
    /// </remarks>
    internal static readonly TimeSpan StaleAfter = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets whether every step of the job has concluded and the job has not moved on since <see cref="StaleAfter"/>.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True if the job is recorded as running with nothing left that could finish it.</returns>
    internal static bool HasConcludedAllStepsLongAgo(JobState job, DateTimeOffset now)
    {
        var progress = job.Progress;
        if (job.Status != JobStatus.Running || progress.TotalSteps <= 0)
        {
            return false;
        }

        var concluded = progress.SuccessfulSteps + progress.FailedSteps + progress.StoppedSteps;
        if (concluded < progress.TotalSteps)
        {
            return false;
        }

        var lastChange = job.StatusChanges.Select(_ => _.Occurred).DefaultIfEmpty(job.Created).Max();
        return now - lastChange > StaleAfter;
    }

    /// <summary>
    /// Gets whether a job was stopped before it prepared a single step.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <returns>True if the job is stopped and has no steps - it carries no work and resuming it can never drive anything.</returns>
    internal static bool NeverPrepared(JobState job) =>
        job.Status == JobStatus.Stopped && job.Progress.TotalSteps <= 0;
}
