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
        await CheckJobTasks();
        if (await CheckNextSequenceNumber())
        {
            await CheckStrandedSubscription();
        }

        await FlushDebouncedProgressState();
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

    async Task CheckJobTasks()
    {
        if (!_subscription.IsSubscribed)
        {
            return;
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
                return;
            }
        }

        if (State.CatchingUpPartitions.Count > 0)
        {
            var catchupJobs = await _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
            var hasRunningCatchupJob = catchupJobs.Any(job =>
                job.Request is CatchUpObserverRequest req &&
                req.ObserverKey == _observerKey &&
                job.IsPreparingOrRunning);

            if (!hasRunningCatchupJob)
            {
                logger.WatchdogCatchupJobMissing();
                await TransitionTo<Routing>();
            }
        }
    }

    async Task<bool> CheckNextSequenceNumber()
    {
        if (!_subscription.IsSubscribed || State.RunningState != ObserverRunningState.Active)
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

    /// <summary>
    /// Recovers an observer that believes it is observing but is no longer on its appended-events queue. The queue
    /// drops subscriptions behind the observer's back when it spills to catch-up under back-pressure; if the spill's
    /// catch-up trigger never succeeds, the observer is left active and behind with nothing driving it forward, and
    /// nothing else reactivates it because it is kept alive. Recovery re-routes through <see cref="Routing"/>, the
    /// same transition the watchdog already uses for a missing job, which re-evaluates the gap, starts catch-up when
    /// one is needed and re-subscribes on the way back into <see cref="Observing"/>.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Only reached for an observer that is subscribed, active and behind on a relevant event. Being behind on its
    /// own is normal — a live delivery in flight looks exactly the same — so the queue is asked whether it still
    /// holds the subscription, and only its answer distinguishes a stranded observer from a busy one. Replaying,
    /// catching-up, failed-partition and preparing-catch-up observers are already being driven forward and are left
    /// alone, and a catch-up job that is preparing or running is never started a second time.
    /// </remarks>
    async Task CheckStrandedSubscription()
    {
        if (State.IsReplaying ||
            State.CatchingUpPartitions.Count > 0 ||
            _isPreparingCatchup ||
            Failures.HasFailedPartitions)
        {
            return;
        }

        var catchupJobs = await _jobsManager.GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>();
        var hasRunningCatchupJob = catchupJobs.Any(job =>
            job.Request is CatchUpObserverRequest request &&
            request.ObserverKey == _observerKey &&
            job.IsPreparingOrRunning);

        if (hasRunningCatchupJob)
        {
            return;
        }

        if (await _appendedEventsQueues.IsSubscribed(_observerKey))
        {
            return;
        }

        logger.WatchdogRescuingStrandedObserver(State.NextEventSequenceNumber);
        await TransitionTo<Routing>();
    }
}
