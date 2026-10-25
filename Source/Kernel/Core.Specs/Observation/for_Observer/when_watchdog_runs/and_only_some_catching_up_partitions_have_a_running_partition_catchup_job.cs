// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_only_some_catching_up_partitions_have_a_running_partition_catchup_job : given.an_observer_with_client_owned_subscription
{
    static readonly Key _partition = new("some-partition", ArrayIndexers.NoIndexers);
    static readonly Key _unownedPartition = new("unowned-partition", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        _stateStorage.State.CatchingUpPartitions.Add(_unownedPartition);

        IImmutableList<JobState> jobs = ImmutableList.Create(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, [])
        });
        _jobsManager.GetJobs(Arg.Any<JobQuery>()).Returns(Task.FromResult(jobs));
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_rescue_the_catching_up_partitions() => _stateStorage.State.CatchingUpPartitions.ShouldBeEmpty();
    [Fact] void should_be_in_active_state() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Active);
}
