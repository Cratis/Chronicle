// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// A missed delivery found while the barrier is up must not rewind the frozen cursor or start an unbounded partition
/// job. It waits for the continuation, which makes it a bounded obligation.
/// </summary>
public class and_a_missed_delivery_is_found_under_the_repair_barrier : given.an_observer_behind_a_repair_barrier
{
    readonly TaskCompletionSource _liveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _liveResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventSequenceNumber _cursorUnderBarrier;
    bool _startedJobUnderBarrier;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL, LastHandledEventSequenceNumber = 9UL, PendingCatchUpRepairs = [] };
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "T" });
        await _observer.Handle("T", [Event(12UL)]);
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(_ =>
        {
            _liveStarted.TrySetResult();
            return _liveResult.Task;
        });
    }

    async Task Because()
    {
        var live = _observer.Handle("Q", [Event(16UL)]);
        await _liveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        // The barrier is raised while T's held-back event is behind the frozen cursor and T has lost its owner.
        _stateStorage.State = _stateStorage.State with
        {
            NextEventSequenceNumber = 14UL,
            CatchingUpPartitions = new HashSet<Key>(),
            PendingCatchUpRepairs = [new("P", 11UL, 14UL)]
        };
        _liveResult.TrySetResult(ObserverSubscriberResult.Ok(16UL));
        await live.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _cursorUnderBarrier = _stateStorage.State.NextEventSequenceNumber;
        _startedJobUnderBarrier = _jobsManager.ReceivedCalls().Any(_ => _.GetMethodInfo().Name == "Start");

        await _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 13UL, succeeded: true);
        await _silo.RunCatchUpContinuation().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
    }

    [Fact] void should_not_rewind_the_frozen_cursor() => _cursorUnderBarrier.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_not_start_a_partition_job_under_the_barrier() => _startedJobUnderBarrier.ShouldBeFalse();
    [Fact] void should_keep_the_cursor_frozen_for_the_successor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_repair_the_missed_delivery_in_a_bounded_range() => _jobsManager.Received(1)
        .Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"T" && _.FromSequenceNumber == (EventSequenceNumber)12UL && _.ToSequenceNumber == (EventSequenceNumber)13UL));
}
