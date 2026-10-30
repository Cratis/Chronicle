// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
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

    /// <summary>
    /// The longest the delay grows to when reminders keep coming out without a retry job.
    /// </summary>
    internal static readonly TimeSpan MaxRetryWithoutJobDelay = TimeSpan.FromMinutes(5);

    static readonly TimeSpan _firstRetryWithoutJobDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The consecutive retries of each failing partition that came out without a retry job. Bounded by the failed
    /// partitions and reset on deactivation: a retry that has to wait longer after a restart is harmless.
    /// </summary>
    readonly Dictionary<FailedPartitionId, int> _retriesWithoutJob = [];

    bool _retryJobSweepRequested;
    int? _retryJobDropBudget;
    HashSet<JobId> _jobsDroppedByLastSweep = [];

    /// <summary>
    /// Gets how many more jobs can be dropped in the current recovery turn. Outside a recovery turn it is a full turn's worth.
    /// </summary>
    int RetryJobDropsAvailable => _retryJobDropBudget ?? MaxRetryJobDropsPerTurn;

    void ConsumeRetryJobDrops(int dropped)
    {
        if (_retryJobDropBudget is not null)
        {
            _retryJobDropBudget = Math.Max(0, _retryJobDropBudget.Value - dropped);
        }
    }

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
    /// Drops a bounded number of the retry jobs that can never run, taking from the recovery turn's shared budget.
    /// </summary>
    /// <returns>True if more jobs remain to be dropped and the budget was used up, so another turn can continue.</returns>
    /// <remarks>
    /// No lock is held: removing a retry job runs its completion callbacks, which call back into the observer.
    /// Deleting returns normally even when the job could not be removed. Jobs dropped by the previous sweep that are
    /// still listed show that dropping makes no progress, and trying again in an immediate turn would only hot-loop.
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

        if (candidates.Exists(_ => _jobsDroppedByLastSweep.Contains(_.Id)))
        {
            logger.DroppingRetryJobsMakesNoProgress();
            _jobsDroppedByLastSweep = [];
            return false;
        }

        var toDrop = candidates.Take(RetryJobDropsAvailable).ToList();
        var results = await Task.WhenAll(toDrop.Select(DropRetryJob));
        var dropped = toDrop.Where((_, index) => results[index]).Select(_ => _.Id).ToHashSet();
        _jobsDroppedByLastSweep = dropped;
        ConsumeRetryJobDrops(dropped.Count);

        // When the budget was not used up - jobs could not be dropped - trying again in the next turn would only hot-loop.
        return candidates.Count > toDrop.Count && RetryJobDropsAvailable == 0;
    }

    /// <summary>
    /// Registers the next retry of a partition whose reminder came out without a retry job, backing off the longer it keeps happening.
    /// </summary>
    /// <param name="failure">The <see cref="FailedPartition"/> to retry.</param>
    /// <returns>An awaitable task.</returns>
    /// <remarks>
    /// Blockers that cannot be deleted, or a start that keeps failing, would otherwise re-arm at the normal delay - 2 seconds,
    /// or a millisecond for an expired deferral - for ever. The delay is never shorter than the normal one and doubles from
    /// 2 seconds up to <see cref="MaxRetryWithoutJobDelay"/>. The count is bounded by the number of failed partitions,
    /// is forgotten once a job comes out, and is reset on deactivation.
    /// </remarks>
    async Task RegisterRetryReminderAfterNoJob(FailedPartition failure)
    {
        if (!_observerExists || !KeyOwnsDefinition) return;

        // Forget the counts of episodes that are no longer failing, so the map stays bounded by the failed partitions.
        foreach (var id in _retriesWithoutJob.Keys.Where(id => !Failures.Partitions.Any(_ => _.Id == id)).ToArray())
        {
            _retriesWithoutJob.Remove(id);
        }

        _retriesWithoutJob.TryGetValue(failure.Id, out var retries);
        retries++;
        _retriesWithoutJob[failure.Id] = retries;
        var backoff = TimeSpan.FromTicks(Math.Min(MaxRetryWithoutJobDelay.Ticks, _firstRetryWithoutJobDelay.Ticks * (1L << Math.Min(retries - 1, 20))));
        var normalDelay = failure.Deferral is { } deferral ? deferral.Until - DateTimeOffset.UtcNow : GetNextRetryDelay(failure, await GetRetryConfiguration());
        var delay = normalDelay > backoff ? normalDelay : backoff;
        await this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), delay, GetRetryReminderPeriod(delay));
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
