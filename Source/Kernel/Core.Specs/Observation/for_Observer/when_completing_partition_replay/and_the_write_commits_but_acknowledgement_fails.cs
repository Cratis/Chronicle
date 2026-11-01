// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_the_write_commits_but_acknowledgement_fails : given.a_partition_replay_covering_a_failure
{
    void Establish()
    {
        _completionStorage.FailOnWrite = 2;
        _completionStorage.LoseAcknowledgement = true;
    }

    async Task Because()
    {
        await CompleteWithFault();
        await _observer.RunWatchdogAsync();
    }

    [Fact] void should_report_the_lost_acknowledgement() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_have_a_durable_hold_or_successor() => _durablyHeldAfterFault.ShouldBeTrue();
    [Fact] void should_start_the_successor_only_once() => CheckSuccessor();
    [Fact] void should_decrement_the_count_only_once() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual(FailedPartitionCount.Zero);
    [Fact] void should_record_the_catch_up_marker() => _completionStorage.PersistedState.CatchingUpPartitions.ShouldContain(_partition);
}
