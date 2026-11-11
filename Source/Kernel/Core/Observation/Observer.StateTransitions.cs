// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.StateMachines;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Resolves the state to enter on activation, retaining quarantine and keeping retired observers
    /// disconnected. Their replay metadata is retained for a fresh subscription, not a reason to start work.
    /// </summary>
    /// <returns>The type of the state to enter.</returns>
    protected override Type ResolveActivationState()
    {
        if (State.RunningState != ObserverRunningState.Quarantined)
        {
            return !_observerExists || IsRetired ? typeof(Disconnected) : base.ResolveActivationState();
        }

        _resumingQuarantine = true;
        return typeof(QuarantinedObserver);
    }

    /// <inheritdoc/>
    protected override async Task OnBeforeEnteringState(IState<ObserverState> state)
    {
        ResetRecoveryFlagsForEntering(state);
        _isQuarantined = state is QuarantinedObserver;
        await _alertMutationLock.WaitAsync();
        try
        {
            if (state is BaseObserverState observerState)
            {
                TrackQuarantineAlertFor(observerState);
                State = State with { RunningState = observerState.RunningState };
                ApplyRunningState(observerState.RunningState);
            }
        }
        finally
        {
            _alertMutationLock.Release();
        }

        BeginTransitionHook();
    }

    /// <inheritdoc/>
    protected override async Task OnAfterEnteringState(IState<ObserverState> state)
    {
        _stateTransitionInProgress = false;
        KeepWhatWasRecordedDuringTransitionHook();
        PublishRunningStateGauge();
        if (state is States.Replay { StartRefusal: { } refusal })
        {
            // Nothing drives this replay forward, so it settles no owed recovery; routing decides that afresh.
            await SettleRefusedReplayStart(refusal);
            return;
        }

        if (state is QuarantinedObserver) await ReconcileWebhookProbe();
        if (SettlesOwedRecovery(state))
        {
            // Settled: the observer is driven forward again, or routing legitimately disconnected an empty
            // explicitly-typed subscription. The initial Disconnected entry still owes its recovery.
            ForgetOwedRecovery();
            return;
        }

        if (ShouldRecoverAfterEnteringDisconnected(state))
        {
            _recoverSubscriptionAfterQuarantine = false;
            if (_subscription.IsSubscribed)
            {
                await RecoverAfterQuarantine();
            }
        }
    }

    void ForgetOwedRecovery()
    {
        _retryRecoveryAfterQuarantine = false;
        _owedRecoveryAttempts = 0;
    }

    bool ShouldRecoverAfterEnteringDisconnected(IState<ObserverState> state) =>
        state is Disconnected && _recoverSubscriptionAfterQuarantine;

    bool SettlesOwedRecovery(IState<ObserverState> state) =>
        state is Observing or States.Replay || IsEmptySubscriptionDisconnect(state);

    bool IsEmptySubscriptionDisconnect(IState<ObserverState> state) =>
        state is Disconnected &&
        !_recoverSubscriptionAfterQuarantine &&
        _subscription.IsSubscribed &&
        !_subscription.EventTypes.Any() &&
        !State.SubscribesToAllEvents;

    void ResetRecoveryFlagsForEntering(IState<ObserverState> state)
    {
        // Any transition supersedes a recovery scheduled for when a requested leave enters Disconnected. Entering
        // Disconnected is how that leave completes, so only that keeps it.
        if (state is not Disconnected)
        {
            _recoverSubscriptionAfterQuarantine = false;
        }

        // A recovery owed after quarantine stays retryable until it settles. Entering a transient state does not
        // settle it: the transition carrying it onward is dropped if persisting that entry fails. A new quarantine
        // supersedes it, since clearing that one owes a recovery of its own.
        if (state is QuarantinedObserver)
        {
            ForgetOwedRecovery();
        }
    }

    void TrackQuarantineAlertFor(BaseObserverState observerState)
    {
        var wasQuarantined = State.RunningState == ObserverRunningState.Quarantined;
        var isQuarantined = observerState.RunningState == ObserverRunningState.Quarantined;
        if (isQuarantined && !wasQuarantined)
        {
            _quarantineEpisodeId = Guid.NewGuid();
            ChangeAlertState();
        }
        else if (!isQuarantined && wasQuarantined)
        {
            if (_quarantineEpisodeId is { } episode && !_alertEndings.ContainsKey(new(episode)))
            {
                RememberQuarantineEnding(Concepts.Alerts.AlertClearedReason.Cleared);
            }

            _quarantineEpisodeId = null;
            ChangeAlertState();
        }
    }
}
