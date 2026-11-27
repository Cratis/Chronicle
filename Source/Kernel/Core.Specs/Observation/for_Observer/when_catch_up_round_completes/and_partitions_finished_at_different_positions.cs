// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// Reproduces https://github.com/Cratis/Chronicle/issues/4583: partition B drains at 9 while A reaches 12.
/// Correct behavior must deliver B's live event at 10 even though A advances the shared catch-up position.
/// </summary>
public class and_partitions_finished_at_different_positions : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _partitionBDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _completeRound = new(TaskCreationOptions.RunContinuationsAsynchronously);

    async Task Establish()
    {
        PersistStartedPartitionCatchUps();
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 9UL };
        await _observer.RegisterCatchingUpPartitions(["A", "B"]);
        _eventSequence.GetTailSequenceNumber().Returns((EventSequenceNumber)12UL);
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(Arg.Any<EventSequenceNumber>(), Arg.Any<IEnumerable<EventType>>())
            .Returns(GetSequenceNumberError.NotFound);
        _subscriber.ClearReceivedCalls();
        _eventSequence.ClearReceivedCalls();
    }

    async Task Because()
    {
        // The job's B step has exhausted its cursor at 9, but the round still owns both partitions.
        var round = CompleteRound();
        await _partitionBDrained.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        try
        {
            await _observer.Handle("B", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 10UL)]);
        }
        finally
        {
            _completeRound.TrySetResult();
            await round.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    async Task CompleteRound()
    {
        _partitionBDrained.TrySetResult();
        await _completeRound.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // CatchUpObserver reports the maximum of A@12 and B@9, not B's lower drained position.
        await _observer.CaughtUp(JobId.NotSet, 12UL, 12UL);
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_hold_the_live_event_for_repair() => _subscriber.DidNotReceive().OnNext(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>());
    [Fact] void should_keep_the_drained_partition_pending() => _stateStorage.State.CatchingUpPartitions.ShouldContain((Key)"B");
    [Fact] void should_report_one_suspected_skip() => _suspectedSkips.ShouldEqual(1);
    [Fact] void should_repair_from_the_missed_event() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"B" && _.FromSequenceNumber == (EventSequenceNumber)10UL && _.ToSequenceNumber == (EventSequenceNumber)12UL && _.IsHandoverRepair));
    [Fact] void should_keep_the_missed_range_durable() => _stateStorage.State.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("B", 10UL, 13UL));
}
