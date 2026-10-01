// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_replaying_observer_with_client_subscription : when_watchdog_runs.given.an_observer_with_client_owned_subscription
{
    async Task Establish()
    {
        var replayJob = new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, [event_type])
        };
        _jobsManager.GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList.Create(replayJob)));
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
        _stateStorage.State = _stateStorage.State with { IsReplaying = true };
        await _observer.TransitionTo<Routing>();

        // Complete the original subscription's deferred recovery before simulating a later disconnect.
        await _silo.TimerRegistry.FireAllAsync();
    }

    protected async Task DisconnectClient()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(false);
        await _observer.RunWatchdogAsync();
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(true);
    }
}
