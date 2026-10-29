// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Properties;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

/// <summary>
/// A job being removed is still draining and still reports back, so its partition stays held (#5378, #5379).
/// </summary>
public class and_a_held_partitions_catch_up_job_is_being_removed : given.an_observer_with_client_owned_subscription
{
    static readonly Key _partition = new("held-partition", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Removing,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, [])
        });
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_keep_the_hold() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
}
