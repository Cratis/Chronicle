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
/// A partition's own catch-up job owns its hold and reports back through the observer, so the watchdog leaves the hold alone (#5379).
/// </summary>
public class and_a_partition_catch_up_job_runs_for_a_held_partition : given.an_observer_with_client_owned_subscription
{
    static readonly Key _partition = new("held-partition", ArrayIndexers.NoIndexers);

    void Establish()
    {
        _connectedClientsGrain.IsConnected(_connectedClient.ConnectionId).Returns(Task.FromResult(true));
        _stateStorage.State.CatchingUpPartitions.Add(_partition);
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new CatchUpObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, EventSequenceNumber.First, [])
        });

        // Subscribing the observer in the fixture already joined the queue once.
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because() => await _observer.RunWatchdogAsync();

    [Fact] void should_keep_the_hold() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_not_re_subscribe_the_observer_to_its_queue() => _appendedEventsQueues
        .DidNotReceive()
        .Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
