// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// The shortest period Orleans accepts for a reminder by default.
    /// </summary>
    static readonly TimeSpan _minimumRetryReminderPeriod = TimeSpan.FromMinutes(1);

    /// <inheritdoc/>
    public async Task FailedPartitionNotRecovered(Key partition, FailedPartitionId? episodeId = null, Guid? deferralId = null)
    {
        if (CannotAcceptUnrecoveredReport()) return;
        if (!IsCurrentRecovery(partition, episodeId, deferralId)) return;
        if (!Failures.TryGet(partition, out var failure) || failure.IsQuarantined) return;
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
            if (CannotApplyRecoveryReport(partition, episodeId, deferralId)) return;
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
        var (outcome, startFailed) = await TryRecoverFailedPartition(partition);
        if (startFailed)
        {
            throw new FailedPartitionRecoveryCouldNotStart(partition, _observerKey);
        }
        return outcome;
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
            if (RecoveryReportsAreIgnored() || !Failures.TryGet(partition, out failure))
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

        var (retryOutcome, _) = await TryRecoverFailedPartition(partition);
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
        var seconds = config.BackoffDelay * Math.Pow(config.ExponentialBackoffDelayFactor, failure.AttemptsInCurrentBudget);
        var maxTime = TimeSpan.FromSeconds(config.MaximumBackoffDelay);

        // Cap before converting: a large attempt count (for example after the retry policy shrank, or with unlimited
        // retries) can make the exponential delay too large for TimeSpan, or infinite.
        if (seconds > config.MaximumBackoffDelay)
        {
            return maxTime;
        }

        var time = TimeSpan.FromSeconds(seconds);

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

    async Task<(PartitionRecoveryOutcome Outcome, bool StartFailed)> TryRecoverFailedPartition(Key partition)
    {
        ThrowIfSealed();
        await ThrowIfKeyDoesNotOwnDefinition();
        await CancelWebhookProbe();
        if (IsRetired) return (PartitionRecoveryOutcome.PartitionNotFound, false);
        if (IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return (PartitionRecoveryOutcome.ObserverQuarantined, false);
        }

        if (!Failures.TryGet(partition, out var failure))
        {
            logger.SkippingFailedPartitionRecoveryBecausePartitionNotFound(partition);
            return (PartitionRecoveryOutcome.PartitionNotFound, false);
        }

        if (failure.IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecausePartitionIsQuarantined(partition);
            return (PartitionRecoveryOutcome.PartitionQuarantined, false);
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
        var startAttempted = false;
        var jobId = await StartRecoverJobForFailedPartition(failure, () => startAttempted = true);
        if (jobId == Cratis.Choreography.Jobs.JobId.NotSet)
        {
            return (PartitionRecoveryOutcome.Started, ReportJobNotStarted());
        }
        return (PartitionRecoveryOutcome.Started, false);

        bool ReportJobNotStarted()
        {
            if (CannotRecordPartitionFailure())
            {
                logger.FailedPartitionRetryDidNotStart(partition, "ObserverNotActive");
                return false;
            }
            if (startAttempted)
            {
                logger.FailedPartitionRetryDidNotStart(partition, "StartFailed");
                return true;
            }
            logger.FailedPartitionRetryDidNotStart(partition, "RetryScheduled");
            return false;
        }
    }

    /// <summary>
    /// Records a failure while serializing source mutations, then transitions outside the lock.
    /// </summary>
    /// <param name="request">The failed delivery and its recovery identity.</param>
    /// <returns>Awaitable task.</returns>
    async Task RecordPartitionFailure(PartitionFailure request)
    {
        var (partition, sequenceNumber, _, _, kind, episodeId, deferralId, context) = request;

        // An activation that does not own the definition is not the observer that failed; it records nothing.
        if (CannotRecordPartitionFailure())
        {
            return;
        }

        var quarantineObserver = false;
        TimeSpan? retryDelayToRegister;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!IsCurrentRecovery(partition, episodeId, deferralId)) return;
            var config = await GetRetryConfiguration();
            using var scope = logger.BeginObserverScope(_observerId, _observerKey);
            var (failure, partitionWasAlreadyFailed) = RegisterFailureAttempt(request);
            await RecordAbandonedDeliveryFence(failure, kind, context.DeliveryFence);
            if (!partitionWasAlreadyFailed)
            {
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            _metrics?.LegacyPartitionRetryAttempt();
            var webhook = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
            if (webhook) SetWebhookFailureMetadata(failure, context.Policy, sequenceNumber);

            var outcome = DecideQuarantineAndRetry(failure, config, webhook, request);
            quarantineObserver = outcome.QuarantineObserver;
            retryDelayToRegister = outcome.RetryDelay;

            // Arm durable webhook backoff before persisting it, including coalesced writes.
            if (failure.Deferral is { } backoff && retryDelayToRegister is not null)
            {
                await RegisterDeferralReminder(failure, backoff);
                retryDelayToRegister = null;
            }

            ChangeAlertState();
            await PersistFailure(partition, failure, retryDelayToRegister);
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
        if (!ShouldParkExpiredWebhookRetry(failure, config))
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
        Failures.MarkPersisted(onlyThisPartition);
        PublishFailedPartitionsGauge();
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
            if (CannotApplyRecoveryReport(partition, episodeId, deferralId)) return false;
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

    async Task<Cratis.Choreography.Jobs.JobId> StartRecoverJobForFailedPartition(FailedPartition failedPartition, Action? onStartAttempted = null)
    {
        if (IsRetired || _removed) return Cratis.Choreography.Jobs.JobId.NotSet;
        if (IsQuarantined)
        {
            logger.SkippingFailedPartitionRecoveryBecauseObserverIsQuarantined();
            return Cratis.Choreography.Jobs.JobId.NotSet;
        }

        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!await IsRecoveryEligible(failedPartition))
        {
            return Cratis.Choreography.Jobs.JobId.NotSet;
        }

        var usesDeferrals = typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);
        if (usesDeferrals && await RecoveryInFlight(failedPartition))
        {
            logger.FailedPartitionRetryDidNotStart(failedPartition.Partition, "RecoveryInFlight");
            return JobId.NotSet;
        }

        logger.TryingToRecoverFailedPartition(failedPartition.Partition);
        var request = CreateRetryFailedPartitionRequest(failedPartition, usesDeferrals);
        if (usesDeferrals)
        {
            return await StartTrackedRecoveryJob(failedPartition, request, onStartAttempted);
        }

        return await StartRecoveryJob(failedPartition, request, onStartAttempted);
    }

    /// <summary>
    /// Checks quarantine, budget and deadline eligibility in their original short-circuit order.
    /// </summary>
    /// <param name="failedPartition">The recovery episode.</param>
    /// <returns>Whether recovery may start.</returns>
    async Task<bool> IsRecoveryEligible(FailedPartition failedPartition) =>
        !(failedPartition.IsQuarantined ||
          await ParkExpiredWebhookRetry(failedPartition, await GetRetryConfiguration()) ||
          !await IsDeferralDue(failedPartition));

    RetryFailedPartitionRequest CreateRetryFailedPartitionRequest(FailedPartition failedPartition, bool usesDeferrals) =>
        new(_observerKey, Definition.Type, failedPartition.Partition, failedPartition.Deferral?.ResumeFrom ?? (usesDeferrals ? failedPartition.RecoveryCursor?.Position : null) ?? failedPartition.LastAttempt.SequenceNumber, Definition.EventTypes)
        {
            ReaderScope = ReaderScope,
            EpisodeId = usesDeferrals ? failedPartition.Id : null,
            DeferralId = failedPartition.Deferral?.Id,
            ReplayEpoch = failedPartition.ReplayEpoch
        };

    async Task<JobId> StartRecoveryJob(FailedPartition failedPartition, RetryFailedPartitionRequest request, Action? onStartAttempted = null)
    {
        var jobId = await _jobsManager.StartOrResumeObserverJobReportingDrops<IRetryFailedPartition, RetryFailedPartitionRequest>(
            logger,
            () => request,
            requestPredicate: r => r.Key == request.Key && r.EpisodeId == request.EpisodeId && r.DeferralId == request.DeferralId,
            onAlreadyRunningJob: null,
            onResume: RecordStartAttempt,
            onStartNew: RecordStartAttempt,
            onResumeRefused: null,
            concludedJobs: null,
            maxJobsToDrop: RetryJobDropsAvailable,
            onJobsDropped: ConsumeRetryJobDrops);

        // The retry reminder was unregistered when it fired. Without a job nothing would retry this partition again.
        if (jobId == JobId.NotSet)
        {
            await RegisterRetryReminderAfterNoJob(failedPartition);
        }
        else
        {
            _retriesWithoutJob.Remove(failedPartition.Id);
        }

        return jobId;

        Task RecordStartAttempt()
        {
            onStartAttempted?.Invoke();
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Checks whether this activation can accept recovery reports.
    /// </summary>
    /// <returns>Whether recovery reports must be ignored.</returns>
    bool RecoveryReportsAreIgnored() => IsRetired || _removed;

    /// <summary>
    /// Checks activation and episode identity before mutating recovery state.
    /// </summary>
    /// <param name="partition">The failed partition.</param>
    /// <param name="episodeId">The reported episode.</param>
    /// <param name="deferralId">The reported deferral.</param>
    /// <returns>Whether the report cannot be applied.</returns>
    bool CannotApplyRecoveryReport(Key partition, FailedPartitionId? episodeId, Guid? deferralId) =>
        RecoveryReportsAreIgnored() || !IsCurrentRecovery(partition, episodeId, deferralId);

    /// <summary>
    /// Checks whether this activation owns the active failure source.
    /// </summary>
    /// <returns>Whether failure recording is prohibited.</returns>
    bool CannotRecordPartitionFailure() => RecoveryReportsAreIgnored() || !KeyOwnsDefinition;

    /// <summary>
    /// Checks the existing running-state fence for an unsuccessful recovery report.
    /// </summary>
    /// <returns>Whether the report must be ignored.</returns>
    bool CannotAcceptUnrecoveredReport() => RecoveryReportsAreIgnored() || State.RunningState == ObserverRunningState.Quarantined;

    /// <summary>
    /// Checks webhook retry-budget expiry without changing its short-circuit order.
    /// </summary>
    /// <param name="failure">The failed partition.</param>
    /// <param name="config">The retry configuration.</param>
    /// <returns>Whether the webhook retry must be parked.</returns>
    bool ShouldParkExpiredWebhookRetry(FailedPartition failure, Observers config) =>
        !(!typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType) ||
          failure.AttemptsInCurrentBudget == 0 || GetRemainingRetryBudget(failure, config) > TimeSpan.Zero);

    void ArmWebhookProbeAfterFailure() =>
        _armWebhookProbe = IsWebhookSubscription && Failures.Partitions.Any(WebhookProbeEligibility.IsEligible);
}
