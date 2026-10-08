// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The shortest period Orleans accepts for a reminder by default.
    /// </summary>
    static readonly TimeSpan _minimumRetryReminderPeriod = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    public async Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind = FailureKind.Unknown)
    {
        if (IsRetired || _removed)
        {
            return;
        }

        var quarantineObserver = false;
        await _alertMutationLock.WaitAsync();
        try
        {
            var config = await configurationProvider.GetFor(_observerKey);
            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            _metrics?.PartitionFailed();
            logger.PartitionFailed(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
            var partitionWasAlreadyFailed = Failures.IsFailed(partition);
            var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind);
            if (!partitionWasAlreadyFailed)
            {
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            _metrics?.PartitionRetryAttempt();
            if (!IsQuarantined)
            {
                quarantineObserver = ShouldQuarantineObserver(config);
                if (!quarantineObserver)
                {
                    if (config.MaxRetryAttempts == 0 || failure.AttemptsInCurrentBudget <= config.MaxRetryAttempts)
                    {
                        var retryDelay = GetNextRetryDelay(failure, config);
                        await this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), retryDelay, GetRetryReminderPeriod(retryDelay));
                    }
                    else
                    {
                        logger.QuarantiningFailedPartition(partition);
                        failures.State.Quarantine(partition);
                        _metrics?.PartitionQuarantined();
                    }
                }
            }

            // Fence old reports before the partition write. A crash between the two writes reports whichever
            // partition level actually committed. Reports alone are coalesced, never these source writes.
            ChangeAlertState();
            await WriteStateAsync();
            await failures.WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }

        // Entering quarantine stops jobs, which can call the AlwaysInterleave recovery callbacks. Never hold the
        // source mutation lock over those calls. The separate reporting turn runs after this transition commits.
        if (quarantineObserver)
        {
            await TransitionTo<QuarantinedObserver>();
        }
    }

    /// <inheritdoc/>
    public Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber) =>
        FailedPartitionRecovered(partition, lastHandledEventSequenceNumber, EventSequenceNumber.Unavailable);

    /// <inheritdoc/>
    /// <remarks>
    /// Live delivery skips a failed partition, so an event appended after the recovery read the partition but before
    /// the failure is cleared reaches the observer only through catch-up. The check for it therefore runs here, after
    /// the failure is cleared, from the furthest event the recovery read - including events the observer's filters
    /// exclude, which are never counted as handled.
    /// </remarks>
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionRecovered(partition);
        await ResolveFailedPartition(partition, lastHandledEventSequenceNumber);
        await StartCatchupJobIfNeeded(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
    }

    /// <inheritdoc/>
    public async Task FailedPartitionNotRecovered(Key partition)
    {
        if (IsRetired || _removed || State.RunningState == ObserverRunningState.Quarantined) return;
        if (!Failures.TryGet(partition, out var failure) || failure.IsQuarantined) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionNotRecovered(partition);
        await RegisterRetryReminder(failure);
    }

    /// <inheritdoc/>
    public async Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber)
    {
        if (IsRetired || _removed) return;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed) return;
            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            logger.FailingPartitionPartiallyRecovered(partition, lastHandledEventSequenceNumber);
            HandleNewLastHandledEvent(lastHandledEventSequenceNumber);
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<PartitionRecoveryOutcome> TryStartRecoverJobForFailedPartition(Key partition)
    {
        ThrowIfSealed();
        if (IsRetired) return PartitionRecoveryOutcome.PartitionNotFound;
        if (IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return PartitionRecoveryOutcome.ObserverQuarantined;
        }

        if (!Failures.TryGet(partition, out var failure))
        {
            logger.SkippingFailedPartitionRecoveryBecausePartitionNotFound(partition);
            return PartitionRecoveryOutcome.PartitionNotFound;
        }

        if (failure.IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecausePartitionIsQuarantined(partition);
            return PartitionRecoveryOutcome.PartitionQuarantined;
        }

        await StartRecoverJobForFailedPartition(failure);
        return PartitionRecoveryOutcome.Started;
    }

    /// <inheritdoc/>
    public async Task<ClearPartitionQuarantineResult> ClearPartitionQuarantine(Key partition, bool retry)
    {
        ThrowIfSealed();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        FailedPartition? failure;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !Failures.TryGet(partition, out failure))
            {
                logger.SkippingClearPartitionQuarantineBecausePartitionNotFound(partition);
                return new(ClearPartitionQuarantineOutcome.NotFound, PartitionRecoveryOutcome.Started);
            }

            if (!failure.IsQuarantined)
            {
                logger.SkippingClearPartitionQuarantineBecausePartitionNotQuarantined(partition);
                return new(ClearPartitionQuarantineOutcome.NotQuarantined, PartitionRecoveryOutcome.Started);
            }

            logger.ClearingPartitionQuarantine(partition);
            failures.State.ClearQuarantine(partition);
            ChangeAlertState();
            await failures.WriteStateAsync();
            _metrics?.PartitionQuarantineCleared();
        }
        finally
        {
            _alertMutationLock.Release();
        }

        if (!retry)
        {
            await RegisterRetryReminder(failure);
            return new(ClearPartitionQuarantineOutcome.Cleared, PartitionRecoveryOutcome.Started);
        }

        var retryOutcome = await TryStartRecoverJobForFailedPartition(partition);
        if (retryOutcome != PartitionRecoveryOutcome.Started)
        {
            await RegisterRetryReminder(failure);
        }

        return new(ClearPartitionQuarantineOutcome.Cleared, retryOutcome);
    }

    /// <inheritdoc/>
    public async Task ClearFailedPartitions()
    {
        ThrowIfSealed();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (Failures.HasFailedPartitions)
        {
            logger.ClearingFailedPartitions(Failures.Partitions.Count());
            await DiscardFailedPartitions(AlertClearedReason.Cleared);
        }

        ScheduleAlertReport();
    }

    static TimeSpan GetNextRetryDelay(FailedPartition failure, Observers config)
    {
        var time = TimeSpan.FromSeconds(config.BackoffDelay * Math.Pow(config.ExponentialBackoffDelayFactor, failure.AttemptsInCurrentBudget));
        var maxTime = TimeSpan.FromSeconds(config.MaximumBackoffDelay);

        if (time > maxTime)
        {
            return maxTime;
        }

        if (time == TimeSpan.Zero)
        {
            return TimeSpan.FromSeconds(config.BackoffDelay);
        }

        return time;
    }

    /// <summary>
    /// Get the period for the reminder that retries a failed partition.
    /// </summary>
    /// <param name="retryDelay">The delay before the next retry attempt.</param>
    /// <returns>The reminder period.</returns>
    /// <remarks>
    /// The reminder is removed as soon as it is received, so the period only matters if its first tick is never
    /// delivered. That happens when persisting the reminder takes longer than the retry delay - the reminder service
    /// then schedules the next tick one full period after the intended one. Keeping the period at the retry delay
    /// (never below the smallest period the reminder service accepts) bounds how late such a retry can be.
    /// </remarks>
    static TimeSpan GetRetryReminderPeriod(TimeSpan retryDelay) =>
        retryDelay > _minimumRetryReminderPeriod ? retryDelay : _minimumRetryReminderPeriod;

    async Task RegisterRetryReminder(FailedPartition failure)
    {
        var config = await configurationProvider.GetFor(_observerKey);
        var retryDelay = GetNextRetryDelay(failure, config);
        await this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), retryDelay, GetRetryReminderPeriod(retryDelay));
    }

    /// <summary>
    /// Forgets every failed partition, in memory and in storage, along with the reminders retrying them.
    /// </summary>
    /// <param name="reason">The ending reason to retain for each discarded episode.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// The updated observer state and failed partitions are both persisted before returning.
    /// </remarks>
    async Task DiscardFailedPartitions(AlertClearedReason reason)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            foreach (var partition in Failures.Partitions.ToArray())
            {
                _alertEndings[partition.Id] = reason;
                await RemoveReminder(partition);
                failures.State.Remove(partition.Partition);
            }

            State = State with { FailedPartitionCount = 0 };
            ChangeAlertState();
            await WriteStateAsync();
            await failures.WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task ResolveFailedPartition(Key partition, EventSequenceNumber? lastHandled = null)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed) return;
            if (lastHandled is not null)
            {
                HandleNewLastHandledEvent(lastHandled);
            }

            if (Failures.TryGet(partition, out var failure))
            {
                _alertEndings[failure.Id] = AlertClearedReason.Recovered;
                failures.State.Remove(partition);
                State = State with { FailedPartitionCount = State.FailedPartitionCount - 1 };
            }

            ChangeAlertState();
            await WriteStateAsync();
            await failures.WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task StartRecoverJobForFailedPartition(FailedPartition failedPartition)
    {
        if (IsRetired || _removed) return;
        if (IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return;
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        var request = new RetryFailedPartitionRequest(_observerKey, Definition.Type, failedPartition.Partition, failedPartition.LastAttempt.SequenceNumber, Definition.EventTypes);
        await _jobsManager.StartOrResumeObserverJobFor<IRetryFailedPartition, RetryFailedPartitionRequest>(
            logger,
            request,
            requestPredicate: r => r.Key == failedPartition.Partition);
    }

    /// <summary>
    /// Check whether the observer's failures have reached a threshold that takes it out of service.
    /// </summary>
    /// <param name="config">The <see cref="Observers"/> configuration holding the thresholds.</param>
    /// <returns>True when the observer should be quarantined, false if not.</returns>
    /// <remarks>
    /// A partition whose last attempt timed out does not count toward either threshold. Quarantining stops retries
    /// and needs an operator to undo, which is the right answer for an observer that is wrong and the wrong answer
    /// for one that is only waiting on a congested kernel - the congestion is transient and takes the retries with it
    /// when it clears. Counting timeouts would let a busy period take healthy projections out of service, and the
    /// operator who then cleared the quarantine would find nothing wrong with them.
    /// </remarks>
    bool ShouldQuarantineObserver(Observers config)
    {
        var failedPartitionCount = Failures.Partitions.Count(_ => _.LastAttempt.Kind != FailureKind.Timeout);
        if (config.QuarantineOnFailedPartitionCount > 0 && failedPartitionCount >= config.QuarantineOnFailedPartitionCount)
        {
            logger.ObserverFailedPartitionCountThresholdReached(failedPartitionCount, config.QuarantineOnFailedPartitionCount);
            return true;
        }

        if (config.QuarantineOnFailedPartitionPercentage > 0.0 && failedPartitionCount > 0)
        {
            var totalObservedPartitions = Failures.Partitions
                .Select(_ => _.Partition)
                .Concat(Failures.ResolvedPartitions.Select(_ => _.Partition))
                .Distinct()
                .Count();

            var failedPartitionRatio = totalObservedPartitions == 0
                ? 0.0
                : (double)failedPartitionCount / totalObservedPartitions;
            if (failedPartitionRatio > config.QuarantineOnFailedPartitionPercentage)
            {
                logger.ObserverFailedPartitionPercentageThresholdExceeded(failedPartitionRatio, config.QuarantineOnFailedPartitionPercentage);
                return true;
            }
        }

        return false;
    }
}
