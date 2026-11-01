// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Monads;

namespace Cratis.Chronicle.Observation.for_Observer.when_completing_partition_replay;

public class and_a_new_replay_starts_while_completion_is_applying : given.a_partition_replay_covering_a_failure
{
    readonly TaskCompletionSource _probeEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<Result<EventSequenceNumber, GetSequenceNumberError>> _probe = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _replayHeldAfterOldCompletion;
    bool _catchupHeldAfterOldCompletion;

    void Establish()
    {
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo((EventSequenceNumber)51UL, Arg.Any<IEnumerable<EventType>>(), Arg.Any<EventSourceId>())
            .Returns(_ =>
            {
                _probeEntered.TrySetResult();
                return _probe.Task;
            });
        EventSequenceHasNextEvent(70UL);
    }

    async Task Because()
    {
        var completion = CompleteReplay();
        await _probeEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        await _observer.ReplayPartition(_partition);
        _probe.SetResult((EventSequenceNumber)51UL);
        await completion;
        _replayHeldAfterOldCompletion = _completionStorage.PersistedState.ReplayingPartitions.Contains(_partition);
        _catchupHeldAfterOldCompletion = _completionStorage.PersistedState.CatchingUpPartitions.Contains(_partition);
        await _observer.PartitionReplayed(_partition, 62UL, 70UL, [_eventType]);
    }

    [Fact] void should_keep_the_new_replay_marker_after_the_old_probe_returns() => _replayHeldAfterOldCompletion.ShouldBeTrue();
    [Fact] void should_not_record_the_old_successor() => _catchupHeldAfterOldCompletion.ShouldBeFalse();
    [Fact] void should_start_only_one_successor() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Any<CatchUpObserverPartitionRequest>());
    [Fact] void should_start_the_successor_from_the_new_report() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == _partition && _.FromSequenceNumber == (EventSequenceNumber)71UL));
}
