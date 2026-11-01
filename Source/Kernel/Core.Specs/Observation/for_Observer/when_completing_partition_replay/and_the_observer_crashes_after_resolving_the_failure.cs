// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_observer_crashes_after_resolving_the_failure : given.a_partition_replay_covering_a_failure
{
    bool _failureWasResolved;
    bool _markerSurvivedCrash;

    void Establish()
    {
        _failureStorage.LoseAcknowledgement = true;

        // 5383a relies on the surviving replay job to report completion again after an observer crash.
        HasOutstandingJobs(new JobState
        {
            Id = JobId.New(),
            Status = JobStatus.Running,
            Request = new ReplayObserverPartitionRequest(_observerKey, ObserverType.Reactor, _partition, 0UL, 50UL, [_eventType])
        });
    }

    async Task Because()
    {
        _failureStorage.FailOnWrite = 1;
        await CompleteWithFault();
        _failureWasResolved = !_failureStorage.PersistedState.HasFailedPartitions;
        await _failureStorage.ReadStateAsync();
        await Crash();
        _markerSurvivedCrash = (await _observer.GetState()).ReplayingPartitions.Contains(_partition);
        await CompleteReplay();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_have_resolved_the_failure_durably() => _failureWasResolved.ShouldBeTrue();
    [Fact] void should_retain_a_durable_hold_after_the_fault() => _durablyHeldAfterFault.ShouldBeTrue();
    [Fact] void should_retain_the_marker_until_the_job_reports_again() => _markerSurvivedCrash.ShouldBeTrue();
    [Fact] void should_start_the_successor_at_the_original_position() => CheckSuccessor();
    [Fact] void should_record_the_successor_durably() => _completionStorage.PersistedState.CatchingUpPartitions.ShouldContain(_partition);
    [Fact] void should_not_decrement_the_count_twice() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
