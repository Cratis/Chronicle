// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;
using Cratis.Monads;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_watchdog_runs;

public class and_in_flight_catchup_failed_to_start_and_quarantined_the_observer : for_Observer.given.an_observer_with_subscription
{
    readonly Key _partition = "in-flight-partition";

    async Task Establish()
    {
        _stateStorage.State.InFlightPartitions.Add(_partition);
        _jobsManager.Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>())
            .Returns(Task.FromResult(Result<JobId, StartJobError>.Failed(StartJobError.Unknown)));
        await _observer.TransitionTo<CatchingUpInFlight>();
        (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
        _jobsManager.ClearReceivedCalls();
        _appendedEventsQueues.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
        await _observer.RunWatchdogAsync();
    }

    [Fact] async Task should_keep_the_quarantined_state() => (await _observer.GetCurrentState()).ShouldBeOfExactType<QuarantinedObserver>();
    [Fact] void should_keep_the_persisted_quarantine() => _stateStorage.State.RunningState.ShouldEqual(ObserverRunningState.Quarantined);
    [Fact] void should_keep_the_unstarted_partition() => _stateStorage.State.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_not_start_replacement_catchup() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Any<CatchUpObserverRequest>());
    [Fact] void should_not_retry_partition_catchup() => CheckDidNotStartCatchupJob();
    [Fact] void should_not_resubscribe() => _appendedEventsQueues.DidNotReceive().Subscribe(Arg.Any<ObserverKey>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<ObserverFilters?>());
}
