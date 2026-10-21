// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Observation.Webhooks;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>The dedicated durable probe wakeup, never a failed partition key.</summary>
    internal const string WebhookProbeReminderName = "webhook-quarantine-probe";
    const int ProbeReadLimit = 32;
    WebhookProbeState? _webhookProbe;
    bool _armWebhookProbe;
    bool _webhookProbeRunning;
    TaskCompletionSource? _webhookProbeCompletion;
    CancellationTokenSource? _webhookProbeCancellation;
    Task<ObserverSubscriberResult>? _webhookProbeDelivery;
    IWebhookObserverSubscriber? _webhookProbeSubscriber;

    /// <summary>Gets or sets the clock used for deterministic probe deadline evaluation.</summary>
    internal TimeProvider ProbeTimeProvider { get; set; } = TimeProvider.System;

    /// <summary>Gets or sets the jitter sample source.</summary>
    internal Func<double> ProbeRandom { get; set; } = Random.Shared.NextDouble;

    bool IsWebhookSubscription => typeof(IWebhookObserverSubscriber).IsAssignableFrom(_subscription.SubscriberType);

    bool WebhookProbeUnavailable => _removed || IsRetired;

    bool HasWebhookProbeAuthority => IsWebhookSubscription || _webhookProbe is not null || _armWebhookProbe;

    bool HasWebhookProbeSubscription => IsWebhookSubscription && _subscription.IsSubscribed && !_subscriptionSetupFailed;

    static bool MatchesWebhookProbeDefinition(WebhookProbeState control, WebhookDefinition definition) =>
        definition.IsActive && WebhookProbeFingerprint(definition) == control.DefinitionFingerprint;

    static void ResetWebhookProbeBudget(FailedPartition failure, Guid recoveryId)
    {
        failure.IsQuarantined = false;
        failure.AttemptsBeforeBudgetReset = failure.Attempts.Count();
        failure.Deferral = null;
        failure.RecoveryOwner = null;
        failure.WebhookRecoveryId = recoveryId;
    }

    static string WebhookProbeFingerprint(WebhookDefinition definition) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(definition))));

    IWebhookObserverSubscriber GetWebhookProbeSubscriber(Key partition)
    {
        var subscription = IsWebhookSubscription
            ? _subscription
            : new ObserverSubscription(_observerId, _observerKey, Definition.EventTypes, typeof(IWebhookObserverSubscriber), SiloAddress.Zero, null);
        return (IWebhookObserverSubscriber)GrainFactory.GetGrain(typeof(IWebhookObserverSubscriber), subscription.GetSubscriberKeyFor(partition, SiloAddress.Zero));
    }

    Task<WebhookProbe> GetWebhookProbeConfiguration()
    {
        var options = optionsMonitor.CurrentValue.Webhooks.Probe;
        options.Validate();
        return Task.FromResult(options);
    }

    DateTimeOffset NextWebhookProbeDue(WebhookProbe options, int failures) =>
        WebhookProbeSchedule.NextDue(options, failures, ProbeRandom(), ProbeTimeProvider.GetUtcNow());

    async Task ArmWebhookProbe(WebhookProbeState control)
    {
        var delay = control.Due - ProbeTimeProvider.GetUtcNow();
        if (delay <= TimeSpan.Zero) delay = TimeSpan.FromMilliseconds(1);
        await this.RegisterOrUpdateReminder(WebhookProbeReminderName, delay, GetRetryReminderPeriod(delay));
    }

    async Task TryArmWebhookProbe(WebhookProbeState control)
    {
        try
        {
            await ArmWebhookProbe(control);
        }
        catch (Exception)
        {
            // The source-owned deadline stays durable; activation and the watchdog retry registration.
            logger.WebhookProbeLocalFailure();
        }
    }

    bool CanRunWebhookProbe(WebhookProbeState control) =>
        IsQuarantined && HasWebhookProbeSubscription && control.QuarantineId == _quarantineEpisodeId;

    async Task ReconcileWebhookProbe()
    {
        if (WebhookProbeUnavailable || !HasWebhookProbeAuthority) return;
        if (_webhookProbe is { Cancelled: true })
        {
            await CancelWebhookProbe(waitForTurn: false);
            return;
        }
        var options = await GetWebhookProbeConfiguration();
        if (!IsQuarantined)
        {
            await ClearCompletedWebhookProbe();
            return;
        }
        await ReconcileQuarantinedWebhookProbe(options);
    }

    async Task ClearCompletedWebhookProbe()
    {
        if (_webhookProbe is not { ResumePending: true }) return;
        _webhookProbe = null;
        await WriteStateAsync();
        await UnregisterReminderNamed(WebhookProbeReminderName);
    }

    bool ShouldArmNewWebhookProbe() => _webhookProbe is null && _armWebhookProbe;

    async Task ReconcileQuarantinedWebhookProbe(WebhookProbe options)
    {
        if (ShouldArmNewWebhookProbe() && _quarantineEpisodeId is { } quarantine)
        {
            await ScheduleNewWebhookProbe(options, quarantine);
        }
        else if (_webhookProbe is { } existing)
        {
            if (options.Enabled) await TryArmWebhookProbe(existing);
        }
        if (!options.Enabled && _webhookProbe is not null)
        {
            await DrainWebhookProbe();
            await UnregisterReminderNamed(WebhookProbeReminderName);
        }
    }

    async Task ScheduleNewWebhookProbe(WebhookProbe options, Guid quarantine)
    {
        var control = new WebhookProbeState(Guid.NewGuid(), quarantine, NextWebhookProbeDue(options, 0), 0);
        _webhookProbe = control;
        _armWebhookProbe = false;
        await WriteStateAsync();
        if (options.Enabled) await TryArmWebhookProbe(control);
        logger.WebhookProbeScheduled(control.Id, control.Due, control.Failures);
        _metrics?.WebhookProbeScheduled();
    }

    async Task ReceiveWebhookProbeReminder()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (_webhookProbeRunning || WebhookProbeUnavailable) return;
        _webhookProbeRunning = true;
        _webhookProbeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await ReconcileWebhookProbe();
            if (!(await GetWebhookProbeConfiguration()).Enabled) return;
            if (_webhookProbe is not { } control || !CanRunWebhookProbe(control)) return;
            if (control.Due > ProbeTimeProvider.GetUtcNow()) return;
            await RunWebhookProbe(control);
        }
        catch (Exception)
        {
            // No exception bodies: transport/security errors may contain credentials or a full URL.
            logger.WebhookProbeLocalFailure();
            if (_webhookProbe is { ResumePending: false } current)
            {
                await RescheduleWebhookProbe(current, null, "local-error");
            }
        }
        finally
        {
            _webhookProbeRunning = false;
            _webhookProbeCompletion.TrySetResult();
        }
    }

    async Task RunWebhookProbe(WebhookProbeState control)
    {
        if (control.ResumePending)
        {
            await CompleteWebhookProbeRecovery(control);
            return;
        }
        var drained = await DrainPendingWebhookProbe(control);
        if (drained is null) return;
        control = drained;
        var failure = WebhookProbeEligibility.Select(Failures.Partitions, control);
        if (failure is null)
        {
            await RescheduleWebhookProbe(control, null, "no-candidate");
            return;
        }
        await ClaimWebhookProbe(control, failure);
    }

    async Task<WebhookProbeState?> DrainPendingWebhookProbe(WebhookProbeState control)
    {
        if (!control.DrainPending || control.Partition is not { } pendingPartition) return control;
        await GetWebhookProbeSubscriber(pendingPartition).Drain();
        if (_webhookProbe?.Id != control.Id || _webhookProbe.Cancelled) return null;
        control = WebhookProbeSchedule.ClearClaim(control);
        _webhookProbe = control;
        await WriteStateAsync();
        return control;
    }

    async Task ClaimWebhookProbe(WebhookProbeState control, FailedPartition failure)
    {
        // Quarantine entry has stopped retry jobs. A persisted claim can have outlived its
        // activation: the same stable subscriber must acknowledge drain before a new call.
        control = control with { EpisodeId = failure.Id, Position = failure.EffectiveRecoveryCursor.Position, Partition = failure.Partition, DrainPending = true };
        _webhookProbe = control;
        await WriteStateAsync();
        var subscriber = GetWebhookProbeSubscriber(failure.Partition);
        await subscriber.Drain();
        if (!CurrentWebhookProbe(control, failure)) return;

        var definition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get(_observerId);
        if (!definition.IsActive || !CurrentWebhookProbe(control, failure)) return;
        var fingerprint = WebhookProbeFingerprint(definition);
        var position = failure.EffectiveRecoveryCursor.Position;
        if (control.Position is { } claimedPosition && claimedPosition != position)
        {
            await RescheduleWebhookProbe(control, null, "stale-cursor");
            return;
        }

        control = control with { EpisodeId = failure.Id, Position = position, DefinitionFingerprint = fingerprint };
        _webhookProbe = control;
        await WriteStateAsync();
        await DeliverWebhookProbe(control, failure, subscriber);
    }

    bool CurrentWebhookProbe(WebhookProbeState control, FailedPartition failure)
    {
        if (WebhookProbeUnavailable || !CanRunWebhookProbe(control)) return false;
        if (_webhookProbe is not { Cancelled: false } current || current.Id != control.Id) return false;
        return Failures.Partitions.Any(_ => _.Id == failure.Id && WebhookProbeEligibility.IsEligible(_) && WebhookProbeEligibility.MatchesPosition(control, _));
    }

    async Task DeliverWebhookProbe(WebhookProbeState control, FailedPartition failure, IWebhookObserverSubscriber subscriber)
    {
        var eventTypes = Definition.EventTypes.ToArray();
        var source = storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).GetEventSequence(_observerKey.EventSequenceId);
        using var cursor = await source.GetEventsWithLimit(failure.EffectiveRecoveryCursor.Position, ProbeReadLimit, eventSourceId: failure.Partition.ToString(), eventTypes: eventTypes);
        if (!await cursor.MoveNext())
        {
            await RescheduleWebhookProbe(control, null, "no-deliverable-event");
            return;
        }

        var selected = PrepareWebhookProbeEvent(failure, cursor.Current.Take(ProbeReadLimit), eventTypes);
        if (selected is null)
        {
            // A bounded filtered-only page cannot demonstrate health or consume the failure.
            await RescheduleWebhookProbe(control, null, "no-deliverable-event");
            return;
        }
        var released = await DecryptEvents([selected]);
        if (released.Length != 1 || released[0].Context.SequenceNumber != selected.Context.SequenceNumber)
        {
            await RescheduleWebhookProbe(control, null, "no-released-event");
            return;
        }
        await DeliverReleasedWebhookProbe(new(control, failure, subscriber, selected, released));
    }

    AppendedEvent? PrepareWebhookProbeEvent(FailedPartition failure, IEnumerable<AppendedEvent> batch, EventType[] eventTypes)
    {
        var recoveryCursor = failure.EffectiveRecoveryCursor;
        var observationState = recoveryCursor.Mode == FailedPartitionRecoveryMode.Replay ? EventObservationState.Replay : EventObservationState.None;
        var nonRedactionTypes = eventTypes.Where(_ => _.Id != GlobalEventTypes.Redaction).Select(_ => _.Id).ToHashSet();
        var events = ObserverEventPreparation.Prepare(observationState, batch, nonRedactionTypes, _subscription.Filters);
        if (events.Length == 0) return null;
        var selected = events[0];
        if (recoveryCursor.ReplayBoundary is { } boundary && selected.Context.SequenceNumber > boundary)
        {
            selected = selected with { Context = selected.Context with { ObservationState = EventObservationState.None } };
        }
        return selected;
    }

    async Task DeliverReleasedWebhookProbe(WebhookProbeDelivery delivery)
    {
        var (control, failure, _, _, _) = delivery;
        var partitionCounts = (await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).ObserverHandledCounts.GetFor(_observerId, failure.Partition)).ToDictionary(_ => _.Key, _ => _.Value);
        if (!CurrentWebhookProbe(control, failure)) return;
        var options = await GetWebhookProbeConfiguration();
        if (!options.Enabled || !CurrentWebhookProbe(control, failure)) return;

        var result = await InvokeWebhookProbe(delivery, options);
        await ApplyWebhookProbeResult(delivery, partitionCounts, result);
    }

    async Task<ObserverSubscriberResult> InvokeWebhookProbe(WebhookProbeDelivery delivery, WebhookProbe options)
    {
        var (control, failure, subscriber, _, released) = delivery;
        using var cancellation = new CancellationTokenSource();
        _webhookProbeCancellation = cancellation;
        _webhookProbeSubscriber = subscriber;
        try
        {
            _metrics?.WebhookProbeAttempt();
            logger.WebhookProbeAttempt(control.Id, failure.Id);
            _webhookProbeDelivery = subscriber.OnNextWithin(TimeSpan.FromSeconds(options.TimeoutSeconds), failure.Partition, released, new ObserverSubscriberContext(_subscription.GenerationDelivery), cancellation.Token);
            var result = await _webhookProbeDelivery;

            // Even a nominal result cannot release quarantine before the completion barrier.
            await subscriber.Drain();
            return result;
        }
        finally
        {
            _webhookProbeCancellation = null;
            _webhookProbeDelivery = null;
            _webhookProbeSubscriber = null;
        }
    }

    async Task ApplyWebhookProbeResult(WebhookProbeDelivery delivery, Dictionary<EventTypeId, EventCount> partitionCounts, ObserverSubscriberResult result)
    {
        var (control, failure, _, selected, _) = delivery;
        if (!CurrentWebhookProbe(control, failure)) return;
        control = control with { DrainPending = false };
        _webhookProbe = control;
        var currentDefinition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get(_observerId);
        if (!CurrentWebhookProbe(control, failure) || !MatchesWebhookProbeDefinition(control, currentDefinition))
        {
            await RescheduleWebhookProbe(control, null, "stale-definition");
            return;
        }

        if (!(await GetWebhookProbeConfiguration()).Enabled)
        {
            await RescheduleWebhookProbe(control, null, "disabled");
            return;
        }
        var delivered = selected.Context.SequenceNumber;
        if (!WebhookProbeOutcome.IsSuccessful(result, delivered))
        {
            await ApplyWebhookProbeFailure(control, failure, result);
            return;
        }
        await CheckpointSuccessfulWebhookProbe(control, selected, partitionCounts);
    }

    async Task ApplyWebhookProbeFailure(WebhookProbeState control, FailedPartition failure, ObserverSubscriberResult result)
    {
        if (result.FailurePolicy == WebhookDeliveryFailurePolicy.ParkPartition)
        {
            failure.WebhookDisposition = WebhookFailureDisposition.Gone;
            failure.IsQuarantined = true;
            await failures.WriteStateAsync();
        }
        await RescheduleWebhookProbe(control, result.Deferral?.Until, WebhookProbeOutcome.FailureReason(result));
    }

    async Task CheckpointSuccessfulWebhookProbe(WebhookProbeState control, AppendedEvent selected, Dictionary<EventTypeId, EventCount> partitionCounts)
    {
        var delivered = selected.Context.SequenceNumber;
        partitionCounts.TryGetValue(selected.Context.EventType.Id, out var previousCount);
        partitionCounts[selected.Context.EventType.Id] = (previousCount ?? EventCount.Zero) + 1UL;
        control = control with { ResumePending = true, ResumePosition = delivered.Next(), PartitionCounts = partitionCounts.Select(_ => new WebhookProbePartitionCount(_.Key, _.Value)).ToArray() };
        _webhookProbe = control;

        // This write makes successful progress authoritative before any fallible local reset/transition.
        State = WithIncrementedRunningTotals(State, new Dictionary<EventTypeId, EventCount> { [selected.Context.EventType.Id] = 1 });
        State = State with
        {
            LastHandledEventSequenceNumber = !State.LastHandledEventSequenceNumber.IsActualValue || State.LastHandledEventSequenceNumber < delivered ? delivered : State.LastHandledEventSequenceNumber
        };
        RecordedHandlingState();
        await WriteStateAsync();
        _metrics?.WebhookProbeSuccess();
        await CompleteWebhookProbeRecovery(control);
    }

    async Task RescheduleWebhookProbe(WebhookProbeState control, DateTimeOffset? receiverDeadline, string reason)
    {
        if (WebhookProbeUnavailable || !WebhookProbeEligibility.CanReschedule(control, _webhookProbe)) return;
        var options = await GetWebhookProbeConfiguration();
        var due = NextWebhookProbeDue(options, WebhookProbeSchedule.NextFailureCount(control));
        var next = WebhookProbeSchedule.Reschedule(control, due, receiverDeadline);
        if (_webhookProbe?.Id != control.Id) return;
        _webhookProbe = next;
        await WriteStateAsync();
        await TryArmWebhookProbe(next);
        _metrics?.WebhookProbeScheduled();
        RecordWebhookProbeReschedule(reason);
        logger.WebhookProbeRescheduled(control.Id, reason, next.Due);
    }

    void RecordWebhookProbeReschedule(string reason)
    {
        if (reason == "deferred") _metrics?.WebhookProbeDeferred();
        else if (WebhookProbeOutcome.IsFailureReason(reason)) _metrics?.WebhookProbeFailure();
        else _metrics?.WebhookProbeSkipped();
    }

    async Task CompleteWebhookProbeRecovery(WebhookProbeState control)
    {
        var failure = Failures.Partitions.FirstOrDefault(_ => _.Id == control.EpisodeId);
        if (failure is null || !CurrentWebhookProbe(control, failure) || control.ResumePosition is not { } next) return;
        var definition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get(_observerId);
        if (!MatchesWebhookProbeDefinition(control, definition)) return;

        // Absolute counts plus the durable success checkpoint make replay of these local writes idempotent.
        await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).ObserverHandledCounts.SetForRecovery(_observerId, failure.Partition, control.PartitionCounts.ToDictionary(_ => _.EventTypeId, _ => _.Count));
        if (!await ResetWebhookProbeFailures(control, failure, next)) return;
        await ResumeAfterWebhookProbe(control, failure);
    }

    async Task<bool> ResetWebhookProbeFailures(WebhookProbeState control, FailedPartition failure, EventSequenceNumber next)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!CurrentWebhookProbe(control, failure)) return false;
            var cursor = failure.EffectiveRecoveryCursor;
            failure.RecoveryCursor = new(next, cursor.Mode, cursor.ReplayBoundary);
            failure.RecoveryOwner = null;
            foreach (var eligible in Failures.Partitions.Where(_ => WebhookProbeEligibility.IsEligible(_) && _.WebhookRecoveryId != control.Id))
            {
                ResetWebhookProbeBudget(eligible, control.Id);
            }
            ChangeAlertState();
            await failures.WriteStateAsync();
            await WriteStateAsync();
            return true;
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    bool IsEligibleWebhookProbeRetryJob(JobState job) =>
        job.Request is Jobs.RetryFailedPartitionRequest request && request.ObserverKey == _observerKey &&
        Failures.Partitions.Any(failed => WebhookProbeEligibility.IsEligible(failed) && failed.Partition == request.Key);

    async Task ResumeAfterWebhookProbe(WebhookProbeState control, FailedPartition failure)
    {
        if (!CurrentWebhookProbe(control, failure)) return;
        var jobs = await _jobsManager.GetUnfinishedJobs();
        foreach (var job in jobs.Where(IsEligibleWebhookProbeRetryJob))
        {
            if (!CurrentWebhookProbe(control, failure)) return;
            await _jobsManager.Delete(job.Id);
        }
        if (!CurrentWebhookProbe(control, failure)) return;
        await ReviveFromQuarantine();
        if (IsQuarantined) return;
        _webhookProbe = null;
        await WriteStateAsync();
        await UnregisterReminderNamed(WebhookProbeReminderName);
        _metrics?.WebhookProbeResumed();
        logger.WebhookProbeResumed(control.Id);
    }

    async Task DrainWebhookProbe()
    {
        if (_webhookProbeCancellation is { } cancellation) await cancellation.CancelAsync();
        if (_webhookProbeDelivery is { } delivery)
        {
            try { await delivery; }
            catch (Exception) { logger.WebhookProbeLocalFailure(); }
        }
        if (_webhookProbeSubscriber is { } subscriber)
        {
            await subscriber.Drain();
        }
        else if (_webhookProbe is { DrainPending: true, Partition: { } partition })
        {
            await GetWebhookProbeSubscriber(partition).Drain();
        }
    }

    async Task PreserveSuccessfulWebhookProbeProgress(WebhookProbeState control)
    {
        if (!control.ResumePending || control.ResumePosition is not { } next || !IsQuarantined) return;
        await _alertMutationLock.WaitAsync();
        try
        {
            var failure = Failures.Partitions.FirstOrDefault(_ => _.Id == control.EpisodeId);
            if (failure is null || failure.EffectiveRecoveryCursor.Position > next) return;
            await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).ObserverHandledCounts.SetForRecovery(_observerId, failure.Partition, control.PartitionCounts.ToDictionary(_ => _.EventTypeId, _ => _.Count));
            var cursor = failure.EffectiveRecoveryCursor;
            failure.RecoveryCursor = new(next, cursor.Mode, cursor.ReplayBoundary);
            failure.RecoveryOwner = null;
            await failures.WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task CancelWebhookProbe(bool waitForTurn = true)
    {
        var control = _webhookProbe;
        if (control is not null)
        {
            if (!control.Cancelled)
            {
                _metrics?.WebhookProbeCancelled();
                logger.WebhookProbeCancelled(control.Id);
            }
            _webhookProbe = control with { Cancelled = true };
            await WriteStateAsync();
        }
        _armWebhookProbe = false;
        await DrainWebhookProbe();
        if (waitForTurn && _webhookProbeCompletion is { } completion) await completion.Task;
        if (control is not null)
        {
            await PreserveSuccessfulWebhookProbeProgress(control);
            _webhookProbe = null;
            await WriteStateAsync();
            await UnregisterReminderNamed(WebhookProbeReminderName);
        }
    }
}
