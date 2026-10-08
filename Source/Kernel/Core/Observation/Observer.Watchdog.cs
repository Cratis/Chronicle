// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation;

public partial class Observer
{
    /// <summary>
    /// Gets the number of consecutive stranded catch-up preparations the watchdog has recovered. This property is for testing purposes only.
    /// </summary>
    internal int CatchupRecoveryAttempts => _catchupRecoveryAttempts;

    /// <summary>
    /// Runs all watchdog checks immediately. This method is for testing purposes only.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    internal Task RunWatchdogAsync() => Watchdog(CancellationToken.None);

    void RegisterWatchdog(int intervalInSeconds)
    {
        this.RegisterGrainTimer(
            Watchdog,
            new GrainTimerCreationOptions
            {
                DueTime = TimeSpan.FromSeconds(intervalInSeconds),
                Period = TimeSpan.FromSeconds(intervalInSeconds)
            });
    }

    async Task Watchdog(CancellationToken cancellationToken)
    {
        using var scope = logger.BeginObserverScope(_observerId, _observerKey);
        if (!_observerExists || _removed || IsRetired)
        {
            await ReconcileAlertsIfNeeded();
            return;
        }

        await CheckConnectedClient();
        await RecoverIfStuck();
        await FlushDebouncedProgressState();
        await ReconcileAlertsIfNeeded();
    }

    /// <summary>
    /// Runs the recovery checks, stopping at the first one that acts on the observer.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Every check here recovers by re-routing the observer, and routing re-evaluates everything the later checks
    /// look at - including starting a fresh catch-up. Running on past the first recovery therefore reads the
    /// aftermath of that recovery as a second fault: a re-route whose catch-up job could not be started leaves the
    /// observer preparing catch-up, which the very next check treats as stranded and recovers again, doubling the
    /// catch-up attempts and the queue unsubscribe/subscribe cycles in a single tick. One tick, one recovery; the
    /// next tick re-evaluates from a settled state.
    /// </remarks>
    async Task RecoverIfStuck()
    {
        if (await CheckOwedRecoveryAfterQuarantine() || await CheckJobTasks() || await CheckStrandedCatchupPreparation())
        {
            return;
        }

        if (await CheckNextSequenceNumber())
        {
            await CheckStrandedSubscription();
        }
    }

    /// <summary>
    /// Retries a recovery owed after a quarantine ended that never reached a settled state.
    /// </summary>
    /// <returns>True if the watchdog acted on the observer, false if there was nothing owed.</returns>
    /// <remarks>
    /// Clearing the quarantine of a subscribed observer, or a subscription arriving while quarantine entry was still
    /// running, owes the observer the recovery a fresh subscription runs. When that recovery fails, or persisting a
    /// transition it scheduled fails, the observer is left disconnected - or stranded in a transient state - and no
    /// longer quarantined. Once the alert reconciliation persists that, nothing shows an operator that clearing again
    /// would help, so the watchdog retries the recovery itself. A failure that retrying cannot fix would turn that
    /// into a silent loop, so the attempts are bounded the same way stranded catch-up preparation is, and the observer
    /// is quarantined again once the bound is exceeded - visible and actionable rather than silently stopped.
    /// </remarks>
    async Task<bool> CheckOwedRecoveryAfterQuarantine()
    {
        if (!CanRetryOwedRecovery() || await GetCurrentState() is not (Disconnected or Routing or CatchingUpInFlight))
        {
            return false;
        }

        var config = await configurationProvider.GetFor(_observerKey);
        if (!CanRetryOwedRecovery())
        {
            return false;
        }

        _owedRecoveryAttempts++;
        if (_owedRecoveryAttempts > config.MaxCatchupRecoveryAttempts)
        {
            logger.GivingUpOnRecoveryAfterQuarantine(_owedRecoveryAttempts, config.MaxCatchupRecoveryAttempts);
            await QuarantineUnrecoverableSubscription();
            return true;
        }

        logger.WatchdogRetryingRecoveryAfterQuarantine(_owedRecoveryAttempts, config.MaxCatchupRecoveryAttempts);
        try
        {
            await RetryRecoveryAfterQuarantine();
        }
        catch (Exception ex)
        {
            // The recovery stays owed and the attempt counted, so the next tick retries it or gives up on it.
            logger.RetryingRecoveryAfterQuarantineFailed(ex);
        }

        return true;
    }

    /// <summary>
    /// Gets whether a recovery owed after quarantine can be retried by the watchdog right now.
    /// </summary>
    /// <returns>True if it is owed and nothing is already recovering the subscription, false if not.</returns>
    bool CanRetryOwedRecovery() =>
        _retryRecoveryAfterQuarantine &&
        _recoveriesInProgress == 0 &&
        !IsQuarantined &&
        _subscription.IsSubscribed;

    async Task QuarantineUnrecoverableSubscription()
    {
        // Only Disconnected may give up on the recovery. A stranded transient state moves there first, without
        // recovering again on the way.
        _recoverSubscriptionAfterQuarantine = false;
        if (await GetCurrentState() is not Disconnected)
        {
            await TransitionTo<Disconnected>();
        }

        if (await GetCurrentState() is Disconnected disconnected)
        {
            await disconnected.QuarantineUnrecoverableSubscription();
        }
    }

    async Task CheckConnectedClient()
    {
        if (!_subscription.IsSubscribed || Definition.Owner != ObserverOwner.Client)
        {
            return;
        }

        // Clients are tracked on the silo terminating their connection - the silo their target
        // names. If that silo is gone, the placement director gives a fresh, empty activation that
        // correctly reports the client as disconnected.
        if (_subscription.Targets.Count > 0)
        {
            foreach (var target in _subscription.Targets.ToArray())
            {
                var connectedClientsForTarget = GrainFactory.GetConnectedClients(target.SiloAddress);
                if (!await connectedClientsForTarget.IsConnected(target.ConnectedClient!.ConnectionId))
                {
                    logger.WatchdogClientInstanceDisconnected(target.ConnectedClient.ConnectionId);
                    RemoveSubscriberTarget(target);
                }
            }

            if (_subscription.Targets.Count == 0)
            {
                await Unsubscribe();
            }

            return;
        }

        if (_subscription.Arguments is not ConnectedClient connectedClient)
        {
            return;
        }

        var connectedClients = GrainFactory.GetConnectedClients(_subscription.SiloAddress);
        if (!await connectedClients.IsConnected(connectedClient.ConnectionId))
        {
            logger.WatchdogClientDisconnected(connectedClient.ConnectionId);
            await Unsubscribe();
        }
    }

    /// <summary>
    /// Re-routes an observer whose progress depends on a job that is no longer there.
    /// </summary>
    /// <returns>True if the observer was re-routed, false if it was left alone.</returns>
    async Task<bool> CheckJobTasks()
    {
        if (IsQuarantined || !_subscription.IsSubscribed)
        {
            return false;
        }

        // Routing defers an explicitly typed replay with no event types, so a deferred replay owns no job yet.
        // An observer already in Replay can still own one, so a missing job is recovered through Routing.
        if (State.IsReplaying &&
            (_subscription.EventTypes.Any() || State.SubscribesToAllEvents || State.RunningState == ObserverRunningState.Replaying))
        {
            var replayJobs = await _jobsManager.GetUnfinishedJobs();
            if (IsQuarantined)
            {
                return false;
            }

            var hasRunningReplayJob = replayJobs.Any(job =>
                job.Request is ReplayObserverRequest req &&
                req.ObserverKey == _observerKey &&
                job.IsPreparingOrRunning);

            if (!hasRunningReplayJob)
            {
                logger.WatchdogReplayJobMissing();
                await TransitionTo<Routing>();
                return true;
            }
        }

        if (State.CatchingUpPartitions.Count > 0)
        {
            var hasRunningCatchupJob = await HasRunningCatchupJob();
            if (IsQuarantined || hasRunningCatchupJob)
            {
                return false;
            }

            logger.WatchdogCatchupJobMissing();
            await TransitionTo<Routing>();
            return true;
        }

        return false;
    }

    async Task<bool> HasRunningCatchupJob()
    {
        // A concluded job being handed over, or a catch-up job still being acquired, owns catch-up although nothing
        // listed does yet: the concluded job is excluded and its successor is not started. CaughtUp interleaves with the
        // watchdog, so a tick can land in that window; rescuing there clears a flag the handover still needs and counts
        // a recovery towards quarantine for a strand that never happened.
        if (HasCatchupOwnershipInFlight())
        {
            return true;
        }

        // A concluded job stays listed until it is finalized but owns nothing; counting it would keep a failed
        // replacement from ever being rescued while that finalization is slow or failed.
        var ownershipEpochBeforeLookup = _catchUpOwnershipEpoch;
        var catchupJobs = await _jobsManager.GetUnfinishedJobs();

        // CaughtUp can begin a handover while the lookup is awaited, so ownership must be checked again. It can also
        // begin and finish there - successor started, nothing left in flight - and then the listing predates the
        // successor, so it cannot say who owns catch-up now. A changed epoch tells.
        if (HasCatchupOwnershipInFlight() || _catchUpOwnershipEpoch != ownershipEpochBeforeLookup)
        {
            return true;
        }

        return catchupJobs.Any(job =>
            job.Request is CatchUpObserverRequest request &&
            request.ObserverKey == _observerKey &&
            job.IsPreparingOrRunning &&
            !_concludedCatchUpJobs.Contains(job.Id));
    }

    bool HasCatchupOwnershipInFlight() => _catchUpHandoversInFlight > 0 || _pendingCatchUpAcquisition is not null;

    async Task<bool> CheckNextSequenceNumber()
    {
        if (IsQuarantined || !_subscription.IsSubscribed || State.RunningState != ObserverRunningState.Active)
        {
            return false;
        }

        if (!State.NextEventSequenceNumber.IsActualValue)
        {
            return false;
        }

        var tailSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        if (!tailSequenceNumber.IsActualValue)
        {
            return false;
        }

        var shouldUpdateTailEventSequenceNumber =
            !State.TailEventSequenceNumber.IsActualValue ||
            State.TailEventSequenceNumber < tailSequenceNumber;

        if (State.NextEventSequenceNumber > tailSequenceNumber)
        {
            if (shouldUpdateTailEventSequenceNumber)
            {
                State = State with { TailEventSequenceNumber = tailSequenceNumber };
                await WriteStateAsync();
            }
            return false;
        }

        var nextEventResult = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(
            State.NextEventSequenceNumber,
            _subscription.EventTypes.ToList());

        var hasRelevantEvent = nextEventResult.Match(num => num.IsActualValue, _ => false);
        if (!hasRelevantEvent)
        {
            logger.WatchdogFastForwardingNextEventSequenceNumber(State.NextEventSequenceNumber, tailSequenceNumber);
            State = State with
            {
                NextEventSequenceNumber = tailSequenceNumber.Next(),
                TailEventSequenceNumber = tailSequenceNumber
            };
            await WriteStateAsync();
            return false;
        }

        if (shouldUpdateTailEventSequenceNumber)
        {
            State = State with { TailEventSequenceNumber = tailSequenceNumber };
            await WriteStateAsync();
        }

        return true;
    }
}
