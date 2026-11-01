// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_failure_resolution_acknowledgement_is_lost_before_reactivation : given.a_partition_replay_covering_a_failure
{
    void Establish()
    {
        _failureStorage.FailOnWrite = 1;
        _failureStorage.LoseAcknowledgement = true;
    }

    async Task Because()
    {
        await CompleteWithFault();
        await Crash();
        _jobsManager.ClearReceivedCalls();
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero);
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_start_one_successor_without_a_job_report() => CheckSuccessor();
    [Fact] void should_clear_the_persisted_intent() => _completionStorage.PersistedState.PartitionReplayCompletions.ShouldBeNull();
    [Fact] void should_not_decrement_the_count_again() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
}
