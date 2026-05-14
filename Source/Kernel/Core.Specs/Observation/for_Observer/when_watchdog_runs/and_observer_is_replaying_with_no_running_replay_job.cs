// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_observer_is_replaying_with_no_running_replay_job : given.an_observer_with_client_owned_subscription
{
    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _stateStorage.State = _stateStorage.State with { IsReplaying = true };

        _jobsManager
            .GetJobsOfType<IReplayObserver, ReplayObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact]
    void should_start_a_new_replay_job() => _jobsManager.Received(1)
        .Start<IReplayObserver, ReplayObserverRequest>(Arg.Any<ReplayObserverRequest>());
}
