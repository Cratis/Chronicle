// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Tracks running observer jobs that have stopped making step progress in this activation.
/// </summary>
internal sealed class StalledObserverJobs
{
    /// <summary>
    /// The maximum number of stalled jobs returned for resuming per watchdog tick.
    /// </summary>
    internal const int MaxResumesPerTick = 16;
    readonly Dictionary<JobId, Mark> _marks = [];

    /// <summary>
    /// Finds the oldest stalled jobs, resetting their stall periods when returned.
    /// </summary>
    /// <param name="jobs">The observer's current unfinished jobs.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The bounded set of jobs to resume.</returns>
    internal IReadOnlyList<JobId> Find(IEnumerable<JobState> jobs, DateTimeOffset now)
    {
        var running = jobs.Where(_ => _.Status == JobStatus.Running).ToDictionary(_ => _.Id);
        foreach (var id in _marks.Keys.Where(_ => !running.ContainsKey(_)).ToArray())
        {
            _marks.Remove(id);
        }

        foreach (var job in running.Values)
        {
            TrackProgress(job, now);
        }

        var stalled = _marks.Where(_ => now - _.Value.Since >= JobLiveness.ResumeStalledAfter)
            .OrderBy(_ => _.Value.Since).Take(MaxResumesPerTick).Select(_ => _.Key).ToArray();
        foreach (var id in stalled)
        {
            _marks[id] = _marks[id] with { Since = now };
        }
        return stalled;
    }

    /// <summary>
    /// Makes a tracked job due on the next <see cref="Find"/>, because its resume attempt did not reach the job.
    /// </summary>
    /// <param name="id">The job to retry.</param>
    /// <remarks>
    /// A job that is not tracked is ignored. A progress change still resets the mark, so a job that moved is not retried.
    /// </remarks>
    internal void RetryOnNextTick(JobId id)
    {
        if (_marks.TryGetValue(id, out var mark))
        {
            _marks[id] = mark with { Since = DateTimeOffset.MinValue };
        }
    }

    void TrackProgress(JobState job, DateTimeOffset now)
    {
        var progress = job.Progress;
        var concluded = progress.SuccessfulSteps + progress.FailedSteps + progress.StoppedSteps + progress.UnreachableSteps;
        if (!_marks.TryGetValue(job.Id, out var mark) || (mark.TotalSteps, mark.Concluded) != (progress.TotalSteps, concluded))
        {
            _marks[job.Id] = new(progress.TotalSteps, concluded, now);
        }
    }

    record Mark(int TotalSteps, int Concluded, DateTimeOffset Since);
}
