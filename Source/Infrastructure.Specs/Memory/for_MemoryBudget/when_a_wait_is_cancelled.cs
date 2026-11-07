// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_a_wait_is_cancelled : Specification
{
    MemoryBudget _budget;
    MemoryReservation _holder;
    Exception _exception;
    int _waitingAfterCancel;
    MemoryReservation _next;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _holder = await _budget.Reserve(900);
        using var cancellation = new CancellationTokenSource();
        var waiting = _budget.Reserve(500, cancellation.Token).AsTask();
        await cancellation.CancelAsync();
        _exception = await Record.ExceptionAsync(() => waiting);
        _waitingAfterCancel = _budget.Waiting;
        _holder.Dispose();
        _next = await _budget.Reserve(500);
    }

    [Fact] void should_throw_cancelled() => _exception.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_stop_waiting() => _waitingAfterCancel.ShouldEqual(0);
    [Fact] void should_not_hold_room_for_the_cancelled_request() => _budget.Reserved.ShouldEqual(500);
}
