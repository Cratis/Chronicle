// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_observer_reactivates_without_a_job_report : given.a_partition_replay_covering_a_failure
{
    void Establish() => _failureStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        await Crash();
        _jobsManager.ClearReceivedCalls();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_start_one_successor_at_the_durable_boundary() => CheckSuccessor();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_resolve_the_original_failure() => _failureStorage.PersistedState.HasFailedPartitions.ShouldBeFalse();
    [Fact] void should_decrement_the_count_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
