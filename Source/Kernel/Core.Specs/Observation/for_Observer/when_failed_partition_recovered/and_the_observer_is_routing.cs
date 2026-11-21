// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Observation.Jobs;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_failed_partition_recovered;

/// <summary>
/// Routing holds a snapshot at the failed position while recovery advances past it. Completing routing with a
/// newer tail must not restore that snapshot's cursor or include the recovered event in a successor catch-up.
/// </summary>
public class and_the_observer_is_routing : for_Observer.given.an_observer_with_subscription_for_specific_event_type
{
    static readonly Key _partition = "recovered-partition";
    static readonly EventSequenceNumber _recovered = 45UL;
    static readonly EventSequenceNumber _movedTail = 50UL;

    TaskCompletionSource _routingEntered;
    TaskCompletionSource<EventSequenceNumber> _tail;
    EventSequenceNumber _cursorAfterRecovery;
    EventSequenceNumber _cursorAfterRouting;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with
        {
            LastHandledEventSequenceNumber = 42UL,
            NextEventSequenceNumber = _recovered,
            TailEventSequenceNumber = _recovered,
            FailedPartitionCount = 1
        };
        _failedPartitionsStorage.State.AddFailedPartition(_partition, _recovered);

        _routingEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _tail = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _eventSequence.GetTailSequenceNumber().Returns(_ =>
        {
            _routingEntered.TrySetResult();
            return _tail.Task;
        });
        _eventSequence.GetNextSequenceNumberGreaterOrEqualTo(_recovered, Arg.Any<IEnumerable<EventType>>()).Returns(_recovered);
    }

    async Task Because()
    {
        var routing = _observer.TransitionTo<Routing>();
        await _routingEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);

        await _observer.FailedPartitionRecovered(_partition, _recovered);
        _cursorAfterRecovery = _stateStorage.State.NextEventSequenceNumber;

        _tail.SetResult(_movedTail);
        await routing.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _cursorAfterRouting = _stateStorage.State.NextEventSequenceNumber;

        // A later observer-wide catch-up must use the merged cursor, not routing's pre-recovery snapshot.
        await _observer.CatchUp();
    }

    [Fact] void should_advance_past_the_recovered_event_before_routing_completes() => _cursorAfterRecovery.ShouldEqual(_recovered.Next());
    [Fact] void should_not_rewind_to_the_recovered_event_after_routing_completes() => _cursorAfterRouting.ShouldEqual(_recovered.Next());
    [Fact] void should_keep_the_recovered_last_handled_position() => _stateStorage.State.LastHandledEventSequenceNumber.ShouldEqual(_recovered);
    [Fact] void should_keep_the_moved_tail() => _stateStorage.State.TailEventSequenceNumber.ShouldEqual(_movedTail);
    [Fact] void should_remove_the_recovered_failure() => _failedPartitionsStorage.State.Partitions.Select(_ => _.Partition).ShouldNotContain(_partition);
    [Fact] void should_have_routed_from_the_pre_recovery_snapshot() => _eventSequence.Received(1).GetNextSequenceNumberGreaterOrEqualTo(_recovered, Arg.Any<IEnumerable<EventType>>());
    [Fact] void should_start_both_catch_ups_after_the_recovered_event() => _jobsManager.Received(2).Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber == _recovered.Next()));
    [Fact] void should_never_request_catch_up_including_the_recovered_event() => _jobsManager.DidNotReceive().Start<ICatchUpObserver, CatchUpObserverRequest>(Arg.Is<CatchUpObserverRequest>(_ => _.FromEventSequenceNumber <= _recovered));
}
