// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The shortest period Orleans accepts for a reminder by default.
    /// </summary>
    static readonly TimeSpan _minimumRetryReminderPeriod = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    public Task PartitionFailed(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind = FailureKind.Unknown,
        FailedPartitionId? episodeId = null,
        Guid? deferralId = null,
        WebhookDeliveryFailurePolicy failurePolicy = WebhookDeliveryFailurePolicy.Retry) =>
        RecordPartitionFailure(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, episodeId, deferralId, new(failurePolicy));

    /// <inheritdoc/>
    public Task PartitionFailedWithDeliveryFence(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind,
        PartitionFailureContext context) =>
        RecordPartitionFailure(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind, null, null, context);

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
    public async Task FailedPartitionRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, EventSequenceNumber lastScannedEventSequenceNumber, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        if (IsRetired || _removed) return;
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!await ResolveFailedPartition(partition, lastHandledEventSequenceNumber, episodeId, deferralId)) return;
        Skips.Completing(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        logger.FailingPartitionRecovered(partition);
        await StartCatchupJobIfNeeded(partition, FurthestOf(lastHandledEventSequenceNumber, lastScannedEventSequenceNumber));
        EvaluateSuspectedSkips();
    }

    /// <inheritdoc/>
    public async Task FailedPartitionNotRecovered(Key partition, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        if (IsRetired || _removed || State.RunningState == ObserverRunningState.Quarantined) return;
        if (!IsCurrentRecovery(partition, episodeId, deferralId) || !Failures.TryGet(partition, out var failure) || failure.IsQuarantined) return;
        ForgetRecoveryStart(partition);
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        logger.FailingPartitionNotRecovered(partition);
        await RegisterRetryReminder(failure);
    }

    /// <inheritdoc/>
    public async Task FailedPartitionPartiallyRecovered(Key partition, EventSequenceNumber lastHandledEventSequenceNumber, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        if (IsRetired || _removed) return;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !IsCurrentRecovery(partition, episodeId, deferralId)) return;
            ForgetRecoveryStart(partition);
            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            logger.FailingPartitionPartiallyRecovered(partition, lastHandledEventSequenceNumber);
            Skips.Delivered(partition, lastHandledEventSequenceNumber);
            HandleNewLastHandledEventForPartition(partition, lastHandledEventSequenceNumber);
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
        await ThrowIfKeyDoesNotOwnDefinition();
        await CancelWebhookProbe();
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

        await _alertMutationLock.WaitAsync();
        try
        {
            if (failure.Deferral is { } deferral)
            {
                failure.RecoveryCursor = new(deferral.ResumeFrom, FailedPartitionRecoveryMode.Live, null);
                failure.Deferral = null;
                await WriteFailedPartitions();
                logger.PartitionDeferralOverridden(partition, deferral.Reason, deferral.Until);
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }

        ForgetRecoveryStart(partition);
        await StartRecoverJobForFailedPartition(failure);
        return PartitionRecoveryOutcome.Started;
    }

    /// <inheritdoc/>
    public async Task<ClearPartitionQuarantineResult> ClearPartitionQuarantine(Key partition, bool retry)
    {
        ThrowIfSealed();
        await ThrowIfKeyDoesNotOwnDefinition();
        await CancelWebhookProbe();
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
            await WriteFailedPartitions();
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
        await ThrowIfKeyDoesNotOwnDefinition();
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (Failures.HasFailedPartitions)
        {
            logger.ClearingFailedPartitions(Failures.Partitions.Count());
            await DiscardFailedPartitions(AlertClearedReason.Cleared);
        }

        ScheduleAlertReport();
    }

    static void SetWebhookFailureMetadata(FailedPartition failure, WebhookDeliveryFailurePolicy failurePolicy, EventSequenceNumber sequenceNumber)
    {
        failure.WebhookDisposition = WebhookProbeOutcome.Disposition(failurePolicy);

        // This deadline belongs to the attempt that just ran, not the next retry.
        failure.Deferral = null;
        failure.RecoveryCursor = new(sequenceNumber, failure.RecoveryCursor?.Mode ?? FailedPartitionRecoveryMode.Live, failure.RecoveryCursor?.ReplayBoundary);
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

    async Task RecordPartitionFailure(
        Key partition,
        EventSequenceNumber sequenceNumber,
        IEnumerable<string> exceptionMessages,
        string exceptionStackTrace,
        FailureKind kind,
        FailedPartitionId? episodeId,
        Guid? deferralId,
        PartitionFailureContext context)
    {
        // An activation that does not own the definition is not the observer that failed; it records nothing.
        if (IsRetired || _removed || !KeyOwnsDefinition)
        {
            return;
        }

        var quarantineObserver = false;
        TimeSpan? retryDelayToRegister = null;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!IsCurrentRecovery(partition, episodeId, deferralId)) return;
            var config = await GetRetryConfiguration();
            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            _metrics?.PartitionFailed();
            logger.PartitionFailed(partition, sequenceNumber, exceptionMessages, exceptionStackTrace);
            var partitionWasAlreadyFailed = Failures.IsFailed(partition);
            var failure = failures.State.RegisterAttempt(partition, sequenceNumber, exceptionMessages, exceptionStackTrace, kind);
            await RecordAbandonedDeliveryFence(failure, kind, context.DeliveryFence);
            if (!partitionWasAlreadyFailed)
            {
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            _metrics?.PartitionRetryAttempt();
            var webhook = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
            if (webhook) SetWebhookFailureMetadata(failure, context.Policy, sequenceNumber);

            if (!IsQuarantined)
            {
                quarantineObserver = webhook ? context.Policy == WebhookDeliveryFailurePolicy.PauseObserver : ShouldQuarantineObserver(config);
                if (webhook && context.Policy == WebhookDeliveryFailurePolicy.ParkPartition)
                {
                    logger.QuarantiningFailedPartition(partition);
                    failures.State.Quarantine(partition);
                    _metrics?.PartitionQuarantined();
                }
                else if (!quarantineObserver)
                {
                    if ((config.MaxRetryAttempts == 0 || failure.AttemptsInCurrentBudget <= config.MaxRetryAttempts) &&
                        (!webhook || GetRemainingRetryBudget(failure, config) > TimeSpan.Zero))
                    {
                        retryDelayToRegister = webhook ? SetWebhookRetryBackoff(failure, config, sequenceNumber) : GetNextRetryDelay(failure, config);
                    }
                    else
                    {
                        logger.QuarantiningFailedPartition(partition);
                        failures.State.Quarantine(partition);
                        _metrics?.PartitionQuarantined();
                    }
                }
            }

            if (webhook && !quarantineObserver)
            {
                quarantineObserver = ShouldQuarantineObserver(config);
            }

            // Arm durable webhook backoff before persisting it, including coalesced writes.
            if (failure.Deferral is { } backoff && retryDelayToRegister is not null)
            {
                await RegisterDeferralReminder(failure, backoff);
                retryDelayToRegister = null;
            }

            ChangeAlertState();
            if (_writeCoalescingInterval > TimeSpan.Zero)
            {
                // Only this partition is written now, and only once: rewriting every failed partition per failure is
                // what made a burst of failures cost O(n) writes each. The reminder and the observer state, which a
                // burst of failures has in common, are written together by the flush.
                await SaveFailedPartition(partition);
                if (retryDelayToRegister is { } pendingDelay)
                {
                    _pendingRetryReminders[failure.Id] = pendingDelay;
                }

                _failureStateWritePending = true;
                EnsureCoalescedWriteTimer();
            }
            else
            {
                if (retryDelayToRegister is { } retryDelay)
                {
                    await this.RegisterOrUpdateReminder(PartitionReminderName(failure.Id), retryDelay, GetRetryReminderPeriod(retryDelay));
                }

                // Fence old reports before the partition write. A crash between the two writes reports whichever
                // partition level actually committed. Reports alone are coalesced, never these source writes.
                await WriteStateAsync();
                await WriteFailedPartitions();
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }

        // Entering quarantine stops jobs, which can call the AlwaysInterleave recovery callbacks. Never hold the
        // source mutation lock over those calls. The separate reporting turn runs after this transition commits.
        if (quarantineObserver)
        {
            ArmWebhookProbeAfterFailure();
            await TransitionTo<QuarantinedObserver>();
        }
    }

    Task<Observers> GetRetryConfiguration() =>
        typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType)
            ? Task.FromResult(optionsMonitor.CurrentValue.Webhooks.Retry)
            : configurationProvider.GetFor(_observerKey);

    TimeSpan GetRemainingRetryBudget(FailedPartition failure, Observers config) =>
        config.RetryBudget <= TimeSpan.Zero
            ? TimeSpan.MaxValue
            : config.RetryBudget - (DateTimeOffset.UtcNow - failure.Attempts.Skip(failure.AttemptsBeforeBudgetReset).First().Occurred);

    async Task<bool> ParkExpiredWebhookRetry(FailedPartition failure, Observers config)
    {
        if (!typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType) ||
            failure.AttemptsInCurrentBudget == 0 || GetRemainingRetryBudget(failure, config) > TimeSpan.Zero)
        {
            return false;
        }

        bool quarantineObserver;
        await _alertMutationLock.WaitAsync();
        try
        {
            failures.State.Quarantine(failure.Partition);
            failure.Deferral = null;
            _metrics?.PartitionQuarantined();
            quarantineObserver = ShouldQuarantineObserver(config);
            ChangeAlertState();
            await WriteStateAsync();
            await WriteFailedPartitions();
        }
        finally
        {
            _alertMutationLock.Release();
        }
        logger.WebhookRetryBudgetExhausted(failure.Partition, config.RetryBudget);
        await RemoveReminder(failure);
        if (quarantineObserver)
        {
            _armWebhookProbe = failure.WebhookDisposition == WebhookFailureDisposition.Retryable;
            await TransitionTo<QuarantinedObserver>();
        }

        return true;
    }

    TimeSpan SetWebhookRetryBackoff(FailedPartition failure, Observers config, EventSequenceNumber sequenceNumber)
    {
        var now = DateTimeOffset.UtcNow;
        var maximum = TimeSpan.FromSeconds(Math.Max(1, config.MaximumBackoffDelay));
        var schedule = config.BackoffScheduleSeconds.Count > 0 ? config.BackoffScheduleSeconds : Configuration.Webhooks.DefaultBackoffScheduleSeconds;
        double basis = schedule[Math.Clamp(failure.AttemptsInCurrentBudget - 1, 0, schedule.Count - 1)];
        basis = Math.Min(basis, maximum.TotalSeconds);
        var jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);
        var delay = TimeSpan.FromSeconds(Math.Clamp(basis * jitter, 0.001, maximum.TotalSeconds));
        var remaining = GetRemainingRetryBudget(failure, config);
        if (delay > remaining)
        {
            delay = remaining > TimeSpan.Zero ? remaining : TimeSpan.FromMilliseconds(1);
        }
        failure.Deferral = new(Guid.NewGuid(), PartitionDeferralReason.RetryBackoff, now, now + delay, sequenceNumber);
        logger.WebhookRetryScheduled(failure.Partition, failure.AttemptsInCurrentBudget, failure.Deferral.Until);
        return delay;
    }

    /// <summary>
    /// Writes a single failed partition to storage, leaving the others as they are.
    /// </summary>
    /// <param name="partition">The partition to write.</param>
    /// <returns>Awaitable task.</returns>
    async Task SaveFailedPartition(Key partition)
    {
        if (!KeyOwnsDefinition || !Failures.TryGet(partition, out var failure))
        {
            return;
        }

        // This bypasses the failed partitions storage provider, so it checks ownership against storage the way the
        // provider does.
        try
        {
            await storage.ThrowIfKeyDoesNotOwnStoredDefinition(_observerKey);
        }
        catch (ObserverKeyDoesNotMatchDefinition)
        {
            await RelinquishOwnershipIfMoved();
            return;
        }

        failure.ObserverId = _observerId;
        var onlyThisPartition = new FailedPartitions { Partitions = [failure] };
        await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).FailedPartitions.Save(_observerId, onlyThisPartition);
    }

    async Task RegisterRetryReminder(FailedPartition failure)
    {
        if (!_observerExists || !KeyOwnsDefinition) return;

        if (failure.Deferral is { } deferral)
        {
            await RegisterDeferralReminder(failure, deferral);
            return;
        }

        var config = await GetRetryConfiguration();
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
        await CancelWebhookProbe();
        await _alertMutationLock.WaitAsync();
        try
        {
            foreach (var partition in Failures.Partitions.ToArray())
            {
                _alertEndings[partition.Id] = reason;
                await RemoveReminder(partition);
                failures.State.Remove(partition.Partition);
                ForgetRecoveryStart(partition.Partition);
            }

            State = State with { FailedPartitionCount = 0 };
            ChangeAlertState();
            await WriteStateAsync();
            await WriteFailedPartitions();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task<bool> ResolveFailedPartition(Key partition, EventSequenceNumber? lastHandled = null, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (IsRetired || _removed || !IsCurrentRecovery(partition, episodeId, deferralId)) return false;
            if (lastHandled is not null)
            {
                HandleNewLastHandledEventForPartition(partition, lastHandled);
            }

            if (Failures.TryGet(partition, out var failure))
            {
                _alertEndings[failure.Id] = AlertClearedReason.Recovered;
                failures.State.Remove(partition);
                ForgetRecoveryStart(partition);
                State = State with { FailedPartitionCount = State.FailedPartitionCount - 1 };
            }

            ChangeAlertState();
            await WriteStateAsync();
            await WriteFailedPartitions();
            return true;
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
        if (failedPartition.IsQuarantined ||
            await ParkExpiredWebhookRetry(failedPartition, await GetRetryConfiguration()) ||
            !await IsDeferralDue(failedPartition))
        {
            return;
        }

        var usesDeferrals = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
        if (usesDeferrals && await RecoveryInFlight(failedPartition)) return;

        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        var request = CreateRetryFailedPartitionRequest(failedPartition, usesDeferrals);
        if (usesDeferrals)
        {
            await StartTrackedRecoveryJob(failedPartition, request);
            return;
        }

        await StartRecoveryJob(request);
    }

    RetryFailedPartitionRequest CreateRetryFailedPartitionRequest(FailedPartition failedPartition, bool usesDeferrals) =>
        new(_observerKey, Definition.Type, failedPartition.Partition, failedPartition.Deferral?.ResumeFrom ?? (usesDeferrals ? failedPartition.RecoveryCursor?.Position : null) ?? failedPartition.LastAttempt.SequenceNumber, Definition.EventTypes)
        {
            ReaderScope = ReaderScope,
            EpisodeId = usesDeferrals ? failedPartition.Id : null,
            DeferralId = failedPartition.Deferral?.Id
        };

    Task<JobId> StartRecoveryJob(RetryFailedPartitionRequest request) =>
        _jobsManager.StartOrResumeObserverJobFor<IRetryFailedPartition, RetryFailedPartitionRequest>(
            logger,
            request,
            requestPredicate: r => r.Key == request.Key && r.EpisodeId == request.EpisodeId && r.DeferralId == request.DeferralId);

    /// <summary>
    /// Check whether the observer's failures have reached a threshold that takes it out of service.
    /// </summary>
    /// <param name="config">The <see cref="Observers"/> configuration holding the thresholds.</param>
    /// <returns>True when the observer should be quarantined, false if not.</returns>
    /// <remarks>
    /// For non-webhook observers, a partition whose last attempt timed out does not count toward either threshold. Quarantining stops retries
    /// and needs an operator to undo, which is the right answer for an observer that is wrong and the wrong answer
    /// for one that is only waiting on a congested kernel - the congestion is transient and takes the retries with it
    /// when it clears. Counting timeouts would let a busy period take healthy projections out of service, and the
    /// operator who then cleared the quarantine would find nothing wrong with them.
    /// </remarks>
    bool ShouldQuarantineObserver(Observers config)
    {
        var webhook = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
        var failedPartitionCount = Failures.Partitions.Count(_ => webhook ? _.IsQuarantined : _.Attempts.Any() && _.LastAttempt.Kind != FailureKind.Timeout);
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
            if (webhook ? failedPartitionRatio >= config.QuarantineOnFailedPartitionPercentage : failedPartitionRatio > config.QuarantineOnFailedPartitionPercentage)
            {
                logger.ObserverFailedPartitionPercentageThresholdExceeded(failedPartitionRatio, config.QuarantineOnFailedPartitionPercentage);
                return true;
            }
        }

        return false;
    }

    void ArmWebhookProbeAfterFailure() =>
        _armWebhookProbe = IsWebhookSubscription && Failures.Partitions.Any(WebhookProbeEligibility.IsEligible);
}
