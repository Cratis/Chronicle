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
        await CheckConnectedClient();
        await RecoverIfStuck();
        await FlushDebouncedProgressState();
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
        if (await CheckJobTasks() || await CheckStrandedCatchupPreparation())
        {
            return;
        }

        if (await CheckNextSequenceNumber())
        {
            await CheckStrandedSubscription();
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
        if (!_subscription.IsSubscribed)
        {
            return false;
        }

        if (State.IsReplaying)
        {
            var replayJobs = await _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>();
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

        if (State.CatchingUpPartitions.Count > 0 && !await HasRunningCatchupJob())
        {
            logger.WatchdogCatchupJobMissing();
            await TransitionTo<Routing>();
            return true;
        }

        return false;
    }

    async Task<bool> HasRunningCatchupJob()
    {
        var catchupJobs = await _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
        return catchupJobs.Any(job =>
            job.Request is CatchUpObserverRequest request &&
            request.ObserverKey == _observerKey &&
            job.IsPreparingOrRunning);
    }

    async Task<bool> CheckNextSequenceNumber()
    {
        if (!_subscription.IsSubscribed || CurrentRunningState != ObserverRunningState.Active)
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
