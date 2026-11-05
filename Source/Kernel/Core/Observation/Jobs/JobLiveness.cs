// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Choreography.Storage.Jobs;

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
    /// How long unchanged step progress is observed before resuming a running job.
    /// </summary>
    /// <remarks>
    /// A step taking longer receives a harmless Resume each period. Keep this well above
    /// <see cref="StaleAfter"/>, the observer watchdog interval and the job engine's step-checkpoint flush interval,
    /// since progress that is persisted late only looks stalled.
    /// </remarks>
    internal static readonly TimeSpan ResumeStalledAfter = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How long a job may stay in preparation without a single step before it is considered abandoned.
    /// </summary>
    /// <remarks>
    /// Preparing counts each step as it is created, so a job that is really preparing has a step count. A job written by
    /// a release that predates the step plan can stay in preparation with none forever (#5667).
    /// </remarks>
    internal static readonly TimeSpan PreparationAbandonedAfter = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets whether a job recorded as preparing never produced a step and has not moved on since <see cref="PreparationAbandonedAfter"/>.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <param name="now">The current time.</param>
    /// <returns>True if the job is preparing with no steps and nothing that could still start them.</returns>
    internal static bool PreparationAbandoned(JobState job, DateTimeOffset now)
    {
        if (job.Status == JobStatus.Running || !job.IsPreparingOrRunning || job.Progress.TotalSteps > 0)
        {
            return false;
        }

        var lastChange = job.StatusChanges.Select(_ => _.Occurred).DefaultIfEmpty(job.Created).Max();
        return now - lastChange > PreparationAbandonedAfter;
    }

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
    /// Gets whether a job was stopped before it ever reached its steps.
    /// </summary>
    /// <param name="job">The <see cref="JobState"/> to check.</param>
    /// <returns>True if the job is stopped and has no steps, or never got as far as starting them - it carries no work, and resuming it can never drive anything.</returns>
    /// <remarks>
    /// This is the same test Cratis.Choreography applies when it refuses to resume a job as not prepared: the status history never
    /// holds <see cref="JobStatus.StartingSteps"/>. The progress is no evidence either way. A job stopped half way through preparing a large
    /// number of steps - a restart while it prepares thousands of partitions - has already counted all of them in
    /// <see cref="JobProgress.TotalSteps"/>, yet no step can have run before the job started its steps, and the refusal is permanent.
    /// Keeping such a job left its observer stranded behind the event sequence for good, because every later catch-up found the same stopped job.
    /// </remarks>
    internal static bool NeverPrepared(JobState job) =>
        job.Status == JobStatus.Stopped && (job.Progress.TotalSteps <= 0 || !job.WasEverPrepared());
}
