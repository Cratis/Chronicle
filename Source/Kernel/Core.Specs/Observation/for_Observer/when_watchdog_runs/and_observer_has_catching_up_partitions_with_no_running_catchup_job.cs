// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_observer_has_catching_up_partitions_with_no_running_catchup_job : given.an_observer_with_client_owned_subscription
{
    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));

        _stateStorage.State.CatchingUpPartitions.Add(new Key("some-partition", ArrayIndexers.NoIndexers));

        _jobsManager
            .GetJobsOfType<ICatchUpObserver, CatchUpObserverRequest>()
            .Returns(Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_clear_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();

    [Fact] void should_be_in_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
