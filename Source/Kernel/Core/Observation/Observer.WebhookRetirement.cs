// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    readonly SemaphoreSlim _webhookRetirementCleanupLock = new(1, 1);
    WebhookRetirement? _webhookRetirement;
    Guid? _webhookRegistrationId;
    string? _webhookRegistrationFingerprint;
    bool _webhookRetirementCleanupPending;
    bool _webhookReactivationPending;
    bool _webhookReplayRetained;

    /// <inheritdoc/>
    public async Task AcceptWebhookRegistration(Guid registrationId, string definitionFingerprint)
    {
        if (_removed || Definition.Type != ObserverType.External) return;
        if (_stateWritingSuspended) throw new InvalidOperationException("Webhook registration cannot be accepted while delivery state is being committed.");
        await _alertMutationLock.WaitAsync();
        try
        {
            if (_webhookRegistrationId == registrationId)
            {
                if (_alertStateNeedsPersistence) await WriteStateAsync();
                return;
            }

            _webhookRegistrationId = registrationId;
            _webhookRegistrationFingerprint = definitionFingerprint;
            ChangeAlertState();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task ReconcileWebhookRegistration() => TryApplyAcceptedWebhookRegistration();

    async Task RetireGoneWebhook(Key partition, EventSequenceNumber sequenceNumber, WebhookDeliveryAttempt? attempt)
    {
        await _alertMutationLock.WaitAsync();
        try
        {
            if (_removed || IsRetired || Definition.Type != ObserverType.External || attempt is null ||
                attempt.LifecycleId == Guid.Empty || attempt.LifecycleId != _alertLifecycleId)
            {
                return;
            }

            var definition = await GetAuthoritativeWebhookDefinition();
            if (definition is null || WebhookDeliveryAttempt.Fingerprint(definition) != attempt.DefinitionFingerprint) return;

            // Fence before awaiting persistence. Failures retain this fence; the watchdog retries persistence
            // before destructive cleanup, and late delivery completions cannot reopen the source.
            var target = new Uri(definition.Target.Url);
            var authority = new UriBuilder(target.Scheme, target.IdnHost, target.IsDefaultPort ? -1 : target.Port).Uri.GetLeftPart(UriPartial.Authority);
            _webhookRetirement = new(Guid.NewGuid(), DateTimeOffset.UtcNow, authority);

            // Registration accepted before this response cannot authorize recovery of the new retirement.
            _webhookRegistrationId = null;
            _webhookRegistrationFingerprint = null;
            _webhookRetirementCleanupPending = true;
            _alertDisposition = AlertDisposition.Retired;
            _quarantineEpisodeId = null;
            ChangeAlertState();

            // Keep the first undelivered event even when other partitions advanced the observer cursor.
            if (!Failures.TryGet(partition, out var failure))
            {
                failure = Failures.RegisterAttempt(partition, sequenceNumber, ["Webhook delivery stopped after HTTP 410 Gone."], string.Empty, FailureKind.Handling);
                State = State with { FailedPartitionCount = State.FailedPartitionCount + 1 };
            }

            SetWebhookFailureMetadata(failure, WebhookDeliveryFailurePolicy.RetireWebhook, sequenceNumber);
            await failures.WriteStateAsync();
            await EnsureAlertState();
            await WriteStateAsync();
        }
        finally
        {
            _alertMutationLock.Release();
        }

        // A job's failure callback must return before that job can be stopped. Likewise, do not drain a probe
        // from inside its own delivery turn.
        this.ScheduleInSeparateTurn(ReconcileWebhookRetirement);
    }

    async Task ReconcileWebhookRetirement()
    {
        var retirement = _webhookRetirement;
        if (retirement is null || _removed) return;
        await _webhookRetirementCleanupLock.WaitAsync();
        try
        {
            // Registration or removal may supersede this incident while cleanup waits for the lock.
            if (_webhookRetirement?.Id != retirement.Id || _removed) return;
            if (!_webhookRetirementCleanupPending)
            {
                await ReconcileRemovedWebhook();
                return;
            }
            await _alertMutationLock.WaitAsync();
            try
            {
                // Retry the retained cursor write too if retirement failed before committing the source level.
                await failures.WriteStateAsync();
                await EnsureAlertState();
                if (_alertStateNeedsPersistence) await WriteStateAsync();
                if (_alertStateNeedsPersistence) return;
            }
            finally
            {
                _alertMutationLock.Release();
            }

            await CancelWebhookProbe(waitForTurn: false);
            _subscription = ObserverSubscription.Unsubscribed;
            ForgetSupersededRegistrations();
            SupersedeRecoveryAfterQuarantine();
            await PauseJobs(includeReplay: true);
            foreach (var partition in Failures.Partitions)
            {
                await UnregisterReminderNamed(PartitionReminderName(partition.Id));
                await UnregisterReminderNamed(partition.Partition.ToString());
            }
            await TransitionTo<Disconnected>();
            await WriteStateAsync();
            _webhookRetirementCleanupPending = false;
            await ReconcileRemovedWebhook();
        }
        finally
        {
            _webhookRetirementCleanupLock.Release();
        }
        await ReportAlertState();
    }

    async Task ReconcileRemovedWebhook()
    {
        // Definition removal is shared by every namespace. Reconcile it even after delivery cleanup,
        // without creating a namespace observer or discarding retained progress and failure history.
        var webhooks = storage.GetEventStore(_observerKey.EventStore).Webhooks;
        if (!await webhooks.Has((WebhookId)_observerId.Value))
        {
            await CommitRetired(removing: true);
        }
    }

    async Task TryApplyAcceptedWebhookRegistration()
    {
        if (_webhookRegistrationFingerprint is null || _removed || _stateWritingSuspended) return;
        await ReconcileWebhookRetirement();
        await _alertMutationLock.WaitAsync();
        try
        {
            var definition = await GetAuthoritativeWebhookDefinition();
            if (definition?.IsActive != true ||
                WebhookDeliveryAttempt.Fingerprint(definition) != _webhookRegistrationFingerprint)
            {
                return;
            }
            if (_webhookRetirement is null)
            {
                _webhookRegistrationFingerprint = null;
                ChangeAlertState();
                await WriteStateAsync();
                return;
            }

            // Reset only recovery budgets, once per accepted intent. Keep cursors, counts and attempt history.
            foreach (var failure in Failures.Partitions.Where(failure => failure.WebhookRecoveryId != _webhookRegistrationId))
            {
                ResetWebhookProbeBudget(failure, _webhookRegistrationId!.Value);
                failure.WebhookDisposition = WebhookFailureDisposition.Retryable;
            }
            await failures.WriteStateAsync();

            var retirement = _webhookRetirement;
            var fingerprint = _webhookRegistrationFingerprint;
            var lifecycle = _alertLifecycleId;
            var replayRetained = _webhookReplayRetained;
            _webhookReplayRetained = State.IsReplaying;
            _webhookReactivationPending = true;
            _alertEndings[retirement.Id] = AlertClearedReason.Cleared;
            _webhookRetirement = null;
            _webhookRegistrationFingerprint = null;
            _alertDisposition = AlertDisposition.Active;
            _alertLifecycleId = Guid.NewGuid();
            ChangeAlertState();
            try
            {
                await WriteStateAsync();
            }
            catch
            {
                _webhookRetirement = retirement;
                _webhookRegistrationFingerprint = fingerprint;
                _alertDisposition = AlertDisposition.Retired;
                _alertLifecycleId = lifecycle;
                _webhookReplayRetained = replayRetained;
                _alertEndings.Remove(retirement.Id);
                ChangeAlertState();
                throw;
            }
            finally
            {
                _webhookReactivationPending = false;
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }
    }

    async Task ResumeRetainedWebhookReplay(int recovery)
    {
        await ResumeJobs(recovery);
        if (IsRecoverySuperseded(recovery)) return;
        var replayState = (await GetStates()).OfType<Replay>().Single();
        if (IsRecoverySuperseded(recovery)) return;
        var currentState = await GetCurrentState();
        if (IsRecoverySuperseded(recovery)) return;
        switch (currentState)
        {
            case Disconnected disconnected:
                await disconnected.ResumeRetainedReplay();
                break;
            case States.Replay:
                await replayState.ResumeRetainedReplay();
                break;
            default:
                await TransitionTo<Replay>();
                break;
        }
    }

    async Task<WebhookDefinition?> GetAuthoritativeWebhookDefinition()
    {
        var webhooks = storage.GetEventStore(_observerKey.EventStore).Webhooks;
        var id = (WebhookId)_observerId.Value;
        return await webhooks.Has(id) ? await webhooks.Get(id) : null;
    }
}
