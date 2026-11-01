// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_newer_failure_episode_exists_on_retry : given.a_partition_replay_covering_a_failure
{
    FailedPartitionId _newEpisode;

    void Establish() => _completionStorage.FailOnWrite = 1;

    async Task Because()
    {
        await CompleteWithFault();
        _failedPartitionsStorage.State.Remove(_partition);
        _newEpisode = _failedPartitionsStorage.State.RegisterAttempt(_partition, 12UL, ["New failure"], string.Empty).Id;
        await _failedPartitionsStorage.WriteStateAsync();
        await _observer.RunWatchdogAsync();
        await CompleteReplay();
    }

    [Fact] void should_keep_the_original_durable_hold_after_the_fault() => _durablyHeldAfterFault.ShouldBeTrue();
    [Fact] void should_leave_the_new_episode_unchanged() => _failureStorage.PersistedState.Partitions.Single().Id.ShouldEqual(_newEpisode);
    [Fact] void should_not_decrement_the_new_episode_count() => _completionStorage.PersistedState.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
    [Fact] void should_not_start_catch_up_for_a_failing_partition() => CheckDidNotStartCatchupJob();
}
