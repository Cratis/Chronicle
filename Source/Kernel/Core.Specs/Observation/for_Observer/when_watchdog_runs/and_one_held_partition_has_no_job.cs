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
/// With one hold owned by a partition job and the other ownerless, the watchdog routes; routing keeps the owned hold and releases the other (#5379).
/// </summary>
public class and_one_held_partition_has_no_job : given.an_observer_with_client_owned_subscription
{
    static readonly Key _ownedPartition = new("owned-partition", ArrayIndexers.NoIndexers);
    static readonly Key _ownerlessPartition = new("ownerless-partition", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State.CatchingUpPartitions.Add(_ownedPartition);
        _stateStorage.State.CatchingUpPartitions.Add(_ownerlessPartition);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _ownedPartition, EventSequenceNumber.First, [])
        });
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_keep_the_owned_hold() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_ownedPartition);
    [Fact] void should_release_the_ownerless_hold() => _stateStorage.State.CatchingUpPartitions.ShouldNotContain(_ownerlessPartition);
}
