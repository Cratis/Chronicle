// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Dropping stopped retry jobs that can never resume.
/// </summary>
/// <remarks>
/// A stopped retry job that was never prepared is refused when resumed, and one that belongs to an old failure episode
/// or deferral is refused by the retry job itself. Neither is terminal, so nothing ever removes them: the first kind
/// blocks its partition's retries and both are loaded by every observer's job lookups.
/// </remarks>
public partial class Observer
{
    /// <summary>
    /// The most retry jobs the observer drops in a single turn.
    /// </summary>
    internal const int MaxRetryJobDropsPerTurn = 10;

    bool _retryJobSweepRequested;

    /// <summary>
    /// Selects the stopped retry jobs of this observer that can never run again.
    /// </summary>
    /// <param name="jobs">The jobs to inspect.</param>
    /// <returns>The jobs that can be dropped, as a materialized list.</returns>
    /// <remarks>
    /// Both criteria only ever grow: a job that was never prepared never becomes prepared, and an old episode or
    /// deferral never becomes current again.
    /// </remarks>
    List<JobState> RetryJobsThatCanNeverRun(IEnumerable<JobState> jobs) =>
        jobs.Where(job =>
            job.Status == JobStatus.Stopped &&
            job.Request is RetryFailedPartitionRequest request &&
            request.ObserverKey == _observerKey &&
            (!job.WasEverPrepared() || (request.EpisodeId is not null && !IsCurrentRecovery(request.Key, request.EpisodeId, request.DeferralId)))).ToList();

    /// <summary>
    /// Drops a bounded number of the retry jobs that can never run.
    /// </summary>
    /// <returns>True if more jobs remain to be dropped.</returns>
    /// <remarks>
    /// No lock is held: removing a retry job runs its completion callbacks, which call back into the observer.
    /// </remarks>
    async Task<bool> DropRetryJobsThatCanNeverRun()
    {
        List<JobState> candidates;
        try
        {
            candidates = RetryJobsThatCanNeverRun(await _jobsManager.GetOutstandingJobs());
        }
        catch (Exception ex)
        {
            logger.CouldNotLookUpRetryJobsThatCanNeverRun(ex);
            return false;
        }

        var dropped = candidates.Take(MaxRetryJobDropsPerTurn).ToList();
        var results = await Task.WhenAll(dropped.Select(DropRetryJob));

        // When nothing could be dropped, trying again in the next turn would only hot-loop.
        return results.Any(_ => _) && candidates.Count > dropped.Count;
    }

    async Task<bool> DropRetryJob(JobState job)
    {
        try
        {
            if (job.Request is RetryFailedPartitionRequest request)
            {
                logger.DroppingRetryJobThatCanNeverRun(job.Id, request.Key);
            }

            await _jobsManager.Delete(job.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.CouldNotDropRetryJobThatCanNeverRun(job.Id, ex);
            return false;
        }
    }
}
