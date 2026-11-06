// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_reserving_more_than_is_available : Specification
{
    MemoryBudget _budget;
    MemoryReservation _first;
    Task<MemoryReservation> _second;
    bool _secondWaitedWhileFirstHeld;
    int _waitingWhileFirstHeld;
    MemoryReservation _secondReservation;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _first = await _budget.Reserve(700);
        _second = _budget.Reserve(500).AsTask();
        await Task.Delay(50);
        _secondWaitedWhileFirstHeld = !_second.IsCompleted;
        _waitingWhileFirstHeld = _budget.Waiting;
        _first.Dispose();
        _secondReservation = await _second.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_wait_while_the_first_is_held() => _secondWaitedWhileFirstHeld.ShouldBeTrue();
    [Fact] void should_report_the_waiting_reservation() => _waitingWhileFirstHeld.ShouldEqual(1);
    [Fact] void should_proceed_once_room_is_given_back() => _secondReservation.Bytes.ShouldEqual(500);
    [Fact] void should_have_nothing_waiting_afterwards() => _budget.Waiting.ShouldEqual(0);
}
