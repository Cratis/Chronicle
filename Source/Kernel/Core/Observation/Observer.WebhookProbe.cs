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

    static bool IsProbeEligible(FailedPartition failure) => !failure.IsResolved && failure.WebhookDisposition is WebhookFailureDisposition.Configuration or WebhookFailureDisposition.Retryable && failure.Hold is null;

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

    DateTimeOffset NextWebhookProbeDue(WebhookProbe options, int failures)
    {
        var seconds = Math.Min(options.MaximumDelaySeconds, options.InitialDelaySeconds * Math.Pow(options.BackoffFactor, Math.Min(failures, 1024)));
        var jitter = ((2 * ProbeRandom()) - 1) * options.JitterRatio;
        seconds = Math.Clamp(seconds * (1 + jitter), 0.001, options.MaximumDelaySeconds);
        var now = ProbeTimeProvider.GetUtcNow();
        var delay = TimeSpan.FromSeconds(seconds);
        return delay > DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now + delay;
    }

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

    async Task ReconcileWebhookProbe()
    {
        if (_removed || IsRetired || (!IsWebhookSubscription && _webhookProbe is null && !_armWebhookProbe)) return;
        if (_webhookProbe is { Cancelled: true })
        {
            await CancelWebhookProbe(waitForTurn: false);
            return;
        }
        var options = await GetWebhookProbeConfiguration();
        if (!IsQuarantined)
        {
            if (_webhookProbe is { ResumePending: true })
            {
                _webhookProbe = null;
                await WriteStateAsync();
                await UnregisterReminderNamed(WebhookProbeReminderName);
            }
            return;
        }
        if (_webhookProbe is null && _armWebhookProbe && _quarantineEpisodeId is { } quarantine)
        {
            var control = new WebhookProbeState(Guid.NewGuid(), quarantine, NextWebhookProbeDue(options, 0), 0);
            _webhookProbe = control;
            _armWebhookProbe = false;
            await WriteStateAsync();
            if (options.Enabled) await TryArmWebhookProbe(control);
            logger.WebhookProbeScheduled(control.Id, control.Due, control.Failures);
            _metrics?.WebhookProbeScheduled();
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

    async Task ReceiveWebhookProbeReminder()
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (_webhookProbeRunning || _removed || IsRetired) return;
        _webhookProbeRunning = true;
        _webhookProbeCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            await ReconcileWebhookProbe();
            if (!(await GetWebhookProbeConfiguration()).Enabled) return;
            if (_webhookProbe is not { } control || !IsQuarantined || !IsWebhookSubscription || !_subscription.IsSubscribed || _subscriptionSetupFailed || control.QuarantineId != _quarantineEpisodeId) return;
            if (control.Due > ProbeTimeProvider.GetUtcNow()) return;
            if (control.ResumePending)
            {
                await CompleteWebhookProbeRecovery(control);
                return;
            }

            if (control.DrainPending && control.Partition is { } pendingPartition)
            {
                await GetWebhookProbeSubscriber(pendingPartition).Drain();
                if (_webhookProbe?.Id != control.Id || _webhookProbe.Cancelled) return;
                control = control with { DrainPending = false, EpisodeId = null, Position = null, Partition = null, DefinitionFingerprint = null };
                _webhookProbe = control;
                await WriteStateAsync();
            }

            var failure = control.EpisodeId is { } episode
                ? Failures.Partitions.FirstOrDefault(_ => _.Id == episode && IsProbeEligible(_))
                : Failures.Partitions.Where(IsProbeEligible).OrderBy(_ => _.EffectiveRecoveryCursor.Position).ThenBy(_ => _.Id.Value).FirstOrDefault();
            if (failure is null)
            {
                await RescheduleWebhookProbe(control, null, "no-candidate");
                return;
            }

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

    bool CurrentWebhookProbe(WebhookProbeState control, FailedPartition failure) =>
        !_removed && !IsRetired && IsQuarantined && _subscription.IsSubscribed && IsWebhookSubscription &&
        _webhookProbe is { Cancelled: false } && !_subscriptionSetupFailed && _webhookProbe.Id == control.Id && control.QuarantineId == _quarantineEpisodeId &&
        Failures.Partitions.Any(_ => _.Id == failure.Id && IsProbeEligible(_) &&
            (control.Position is null || _.EffectiveRecoveryCursor.Position == control.Position ||
                (control.ResumePending && _.EffectiveRecoveryCursor.Position == control.ResumePosition)));

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

        var recoveryCursor = failure.EffectiveRecoveryCursor;
        var observationState = recoveryCursor.Mode == FailedPartitionRecoveryMode.Replay ? EventObservationState.Replay : EventObservationState.None;
        var nonRedactionTypes = eventTypes.Where(_ => _.Id != GlobalEventTypes.Redaction).Select(_ => _.Id).ToHashSet();
        var events = ObserverEventPreparation.Prepare(observationState, cursor.Current.Take(ProbeReadLimit), nonRedactionTypes, _subscription.Filters);
        if (events.Length == 0)
        {
            // A bounded filtered-only page cannot demonstrate health or consume the failure.
            await RescheduleWebhookProbe(control, null, "no-deliverable-event");
            return;
        }

        var selected = events[0];
        if (recoveryCursor.ReplayBoundary is { } boundary && selected.Context.SequenceNumber > boundary)
        {
            selected = selected with { Context = selected.Context with { ObservationState = EventObservationState.None } };
        }
        var released = await DecryptEvents([selected]);
        if (released.Length != 1 || released[0].Context.SequenceNumber != selected.Context.SequenceNumber)
        {
            await RescheduleWebhookProbe(control, null, "no-released-event");
            return;
        }
        var partitionCounts = (await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).ObserverHandledCounts.GetFor(_observerId, failure.Partition)).ToDictionary(_ => _.Key, _ => _.Value);
        if (!CurrentWebhookProbe(control, failure)) return;
        var options = await GetWebhookProbeConfiguration();
        if (!options.Enabled || !CurrentWebhookProbe(control, failure)) return;

        using var cancellation = new CancellationTokenSource();
        _webhookProbeCancellation = cancellation;
        _webhookProbeSubscriber = subscriber;
        ObserverSubscriberResult result;
        try
        {
            _metrics?.WebhookProbeAttempt();
            logger.WebhookProbeAttempt(control.Id, failure.Id);
            _webhookProbeDelivery = subscriber.OnNextWithin(TimeSpan.FromSeconds(options.TimeoutSeconds), failure.Partition, released, new ObserverSubscriberContext(_subscription.GenerationDelivery), cancellation.Token);
            result = await _webhookProbeDelivery;

            // Even a nominal result cannot release quarantine before the completion barrier.
            await subscriber.Drain();
        }
        finally
        {
            _webhookProbeCancellation = null;
            _webhookProbeDelivery = null;
            _webhookProbeSubscriber = null;
        }

        if (!CurrentWebhookProbe(control, failure)) return;
        control = control with { DrainPending = false };
        _webhookProbe = control;
        var currentDefinition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get(_observerId);
        if (!CurrentWebhookProbe(control, failure) || !currentDefinition.IsActive || WebhookProbeFingerprint(currentDefinition) != control.DefinitionFingerprint)
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
        if (result.State != ObserverSubscriberState.Ok || result.LastSuccessfulObservation != delivered)
        {
            if (result.FailurePolicy == WebhookDeliveryFailurePolicy.ParkPartition)
            {
                failure.WebhookDisposition = WebhookFailureDisposition.Gone;
                failure.IsQuarantined = true;
                await failures.WriteStateAsync();
            }
            await RescheduleWebhookProbe(control, result.Deferral?.Until, result.State == ObserverSubscriberState.Deferred ? "deferred" : "failed");
            return;
        }

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
        if (_webhookProbe?.Id != control.Id || _removed || IsRetired || control.ResumePending || control.Cancelled || _webhookProbe.Cancelled) return;
        var options = await GetWebhookProbeConfiguration();
        var failedProbes = control.Failures < int.MaxValue ? control.Failures + 1 : int.MaxValue;
        var due = NextWebhookProbeDue(options, failedProbes);
        if (receiverDeadline is { } deadline && deadline > due) due = deadline;
        var next = control with { Due = due, Failures = failedProbes, EpisodeId = control.DrainPending ? control.EpisodeId : null, Position = control.DrainPending ? control.Position : null, Partition = control.DrainPending ? control.Partition : null, DefinitionFingerprint = control.DrainPending ? control.DefinitionFingerprint : null };
        if (_webhookProbe?.Id != control.Id) return;
        _webhookProbe = next;
        await WriteStateAsync();
        await TryArmWebhookProbe(next);
        _metrics?.WebhookProbeScheduled();
        if (reason == "deferred") _metrics?.WebhookProbeDeferred();
        else if (reason == "failed" || reason == "local-error") _metrics?.WebhookProbeFailure();
        else _metrics?.WebhookProbeSkipped();
        logger.WebhookProbeRescheduled(control.Id, reason, due);
    }

    async Task CompleteWebhookProbeRecovery(WebhookProbeState control)
    {
        var failure = Failures.Partitions.FirstOrDefault(_ => _.Id == control.EpisodeId);
        if (failure is null || !CurrentWebhookProbe(control, failure) || control.ResumePosition is not { } next) return;
        var definition = await storage.GetEventStore(_observerKey.EventStore).Webhooks.Get(_observerId);
        if (!definition.IsActive || WebhookProbeFingerprint(definition) != control.DefinitionFingerprint) return;

        // Absolute counts plus the durable success checkpoint make replay of these local writes idempotent.
        await storage.GetEventStore(_observerKey.EventStore).GetNamespace(_observerKey.Namespace).ObserverHandledCounts.SetForRecovery(_observerId, failure.Partition, control.PartitionCounts.ToDictionary(_ => _.EventTypeId, _ => _.Count));
        await _alertMutationLock.WaitAsync();
        try
        {
            if (!CurrentWebhookProbe(control, failure)) return;
            var cursor = failure.EffectiveRecoveryCursor;
            failure.RecoveryCursor = new(next, cursor.Mode, cursor.ReplayBoundary);
            failure.RecoveryOwner = null;
            foreach (var eligible in Failures.Partitions.Where(IsProbeEligible))
            {
                if (eligible.WebhookRecoveryId == control.Id) continue;
                eligible.IsQuarantined = false;
                eligible.AttemptsBeforeBudgetReset = eligible.Attempts.Count();
                eligible.Deferral = null;
                eligible.RecoveryOwner = null;
                eligible.WebhookRecoveryId = control.Id;
            }
            ChangeAlertState();
            await failures.WriteStateAsync();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }

        if (!CurrentWebhookProbe(control, failure)) return;
        var jobs = await _jobsManager.GetUnfinishedJobs();
        foreach (var job in jobs.Where(_ => _.Request is Jobs.RetryFailedPartitionRequest request && request.ObserverKey == _observerKey && Failures.Partitions.Any(failed => IsProbeEligible(failed) && failed.Partition == request.Key)))
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
