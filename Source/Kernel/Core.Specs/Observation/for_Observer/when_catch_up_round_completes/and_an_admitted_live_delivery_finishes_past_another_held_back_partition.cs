// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Jobs;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

/// <summary>
/// The acceptance leaves a held-back event at or after the frozen cursor to the successor. A live delivery admitted
/// before the acceptance must not move the cursor past that event, as nothing else owns it.
/// </summary>
public class and_an_admitted_live_delivery_finishes_past_another_held_back_partition : given.an_observer_with_skip_detection
{
    readonly TaskCompletionSource _liveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _liveResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    EventSequenceNumber _cursorAfterLiveDelivery;

    async Task Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL };
        await _observer.RegisterCatchingUpPartitions(new HashSet<Key> { "P", "T" });
        await _observer.Handle("P", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 11UL)]);
        await _observer.Handle("T", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 15UL)]);
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(_ =>
        {
            _liveStarted.TrySetResult();
            return _liveResult.Task;
        });
    }

    async Task Because()
    {
        var live = _observer.Handle("Q", [AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 16UL)]);
        await _liveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        var acceptance = _observer.CaughtUp(JobId.NotSet, 13UL, 13UL);
        _liveResult.TrySetResult(ObserverSubscriberResult.Ok(16UL));
        await Task.WhenAll(live, acceptance).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _cursorAfterLiveDelivery = _stateStorage.State.NextEventSequenceNumber;
        await _silo.RunCatchUpContinuation();
    }

    [Fact] void should_not_move_the_cursor_past_the_held_back_event() => _cursorAfterLiveDelivery.ShouldEqual((EventSequenceNumber)15UL);
    [Fact] void should_leave_the_held_back_event_to_the_successor() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)15UL);
    [Fact] void should_bound_the_repair_at_the_held_back_event() => _stateStorage.State.PendingCatchUpRepairs!.ShouldContainOnly(new PendingCatchUpRepair("P", 11UL, 15UL));
    [Fact] void should_start_the_bounded_repair_only() => _jobsManager.Received(1).Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"P" && _.ToSequenceNumber == (EventSequenceNumber)14UL));
    [Fact] void should_not_give_the_held_back_event_a_second_owner() => _jobsManager.DidNotReceive().Start<ICatchUpObserverPartition, CatchUpObserverPartitionRequest>(Arg.Is<CatchUpObserverPartitionRequest>(_ => _.Key == (Key)"T"));
}
