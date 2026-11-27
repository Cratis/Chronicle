// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.for_Observer.when_catch_up_round_completes;

public class and_a_repair_acknowledgement_waits_for_live_state_writes : given.an_observer_behind_a_repair_barrier
{
    readonly TaskCompletionSource _liveStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<ObserverSubscriberResult> _liveResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _acknowledgedWhileSuspended;
    EventSequenceNumber _cursorDuringAcknowledgement;

    void Establish()
    {
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 10UL, LastHandledEventSequenceNumber = 9UL, PendingCatchUpRepairs = [] };
        _subscriber.OnNext((Key)"Q", Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<ObserverSubscriberContext>()).Returns(_ =>
        {
            _liveStarted.TrySetResult();
            return _liveResult.Task;
        });
    }

    async Task Because()
    {
        var live = _observer.Handle("Q", [Event(12UL)]);
        await _liveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        _stateStorage.State = _stateStorage.State with { NextEventSequenceNumber = 14UL, PendingCatchUpRepairs = [new("P", 11UL, 14UL)] };
        var acknowledgement = _observer.HandoverRepairCaughtUp("P", 11UL, 14UL, 13UL, succeeded: true);
        try
        {
            _acknowledgedWhileSuspended = acknowledgement.IsCompleted;
            await _observer.Handle("P", [Event(15UL)]);
            _cursorDuringAcknowledgement = _stateStorage.State.NextEventSequenceNumber;
        }
        finally
        {
            _liveResult.TrySetResult(ObserverSubscriberResult.Ok(12UL));
            await Task.WhenAll(live, acknowledgement).WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
    }

    [Fact] void should_not_acknowledge_an_unwritten_completion() => _acknowledgedWhileSuspended.ShouldBeFalse();
    [Fact] void should_keep_the_cursor_frozen_while_the_completion_is_becoming_durable() => _cursorDuringAcknowledgement.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_not_deliver_the_suffix_while_acknowledging() => _delivered.Count.ShouldEqual(0);
    [Fact] void should_not_rewind_when_the_older_live_write_finishes() => _stateStorage.State.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)14UL);
    [Fact] void should_not_leave_a_repair_obligation_after_the_durable_write() => _stateStorage.State.PendingCatchUpRepairs!.Count.ShouldEqual(0);
}
