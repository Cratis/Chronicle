// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_observer_is_replaying_with_running_replay_job : given.an_observer_with_client_owned_subscription
{
    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _stateStorage.State = _stateStorage.State with { IsReplaying = true };

        var runningJob = new JobState
        {
            Id = JobId.New(),
            Request = new ReplayObserverRequest(_observerKey, ObserverType.Reactor, []),
            StatusChanges = [new JobStatusChanged { Status = JobStatus.Running, Occurred = DateTimeOffset.UtcNow }]
        };

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty.Add(runningJob)));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_remain_in_replaying_state() => _stateStorage.State.IsReplaying.ShouldBeTrue();
}
