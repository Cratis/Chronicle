// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_new_replay_starts_after_a_failed_completion : given.a_partition_replay_covering_a_failure
{
    bool _replayHeldAfterWatchdog;
    bool _catchupHeldAfterWatchdog;
    bool _failureRetainedAfterWatchdog;

    void Establish()
    {
        _completionStorage.FailOnWrite = 1;
        EventSequenceHasNextEvent(70UL);
    }

    async Task Because()
    {
        await CompleteWithFault();
        await _observer.ReplayPartition(_partition);
        await _observer.RunWatchdogAsync();
        _replayHeldAfterWatchdog = _completionStorage.PersistedState.ReplayingPartitions.Contains(_partition);
        _catchupHeldAfterWatchdog = _completionStorage.PersistedState.CatchingUpPartitions.Contains(_partition);
        _failureRetainedAfterWatchdog = _failureStorage.PersistedState.IsFailed(_partition);
        await _observer.PartitionReplayed(_partition, 62UL, 70UL, [_eventType]);
    }

    [Fact] void should_have_failed_the_original_completion() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_keep_the_new_replay_marker_during_the_watchdog() => _replayHeldAfterWatchdog.ShouldBeTrue();
    [Fact] void should_not_record_a_stale_successor_during_the_watchdog() => _catchupHeldAfterWatchdog.ShouldBeFalse();
    [Fact] void should_leave_the_failure_for_the_new_replay_to_resolve() => _failureRetainedAfterWatchdog.ShouldBeTrue();
    [Fact] void should_start_only_one_successor() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_start_the_successor_from_the_new_report() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition && _.FromSequenceNumber == (EventSequenceNumber)71UL));
}
