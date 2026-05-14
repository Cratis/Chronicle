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
        await CheckNextSequenceNumber();
    }

    async Task CheckConnectedClient()
    {
        if (!_subscription.IsSubscribed || Definition.Owner != ObserverOwner.Client)
        {
            return;
        }

        if (_subscription.Arguments is not ConnectedClient connectedClient)
        {
            return;
        }

        var connectedClients = GrainFactory.GetGrain<IConnectedClients>(0);
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

    async Task CheckNextSequenceNumber()
    {
        if (!_subscription.IsSubscribed || State.RunningState != ObserverRunningState.Active)
        {
            return;
        }

        if (!State.NextEventSequenceNumber.IsActualValue)
        {
            return;
        }

        var tailSequenceNumber = await _eventSequence.GetTailSequenceNumber();
        if (!tailSequenceNumber.IsActualValue || State.NextEventSequenceNumber > tailSequenceNumber)
        {
            return;
        }

        var nextEventResult = await _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(
            State.NextEventSequenceNumber,
            _subscription.EventTypes.ToList());

        var hasRelevantEvent = nextEventResult.Match(num => num.IsActualValue, _ => false);
        if (!hasRelevantEvent)
        {
            logger.WatchdogFastForwardingNextEventSequenceNumber(State.NextEventSequenceNumber, tailSequenceNumber);
            State = State with { NextEventSequenceNumber = tailSequenceNumber.Next() };
            await WriteStateAsync();
        }
    }
}
