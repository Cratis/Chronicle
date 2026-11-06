// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_requests_arrive_in_order : Specification
{
    MemoryBudget _budget;
    MemoryReservation _holder;
    Task<MemoryReservation> _large;
    Task<MemoryReservation> _small;
    bool _smallJumpedTheQueue;
    MemoryReservation _largeReservation;
    MemoryReservation _smallReservation;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _holder = await _budget.Reserve(600);
        _large = _budget.Reserve(800).AsTask();
        await Task.Delay(30);
        _small = _budget.Reserve(100).AsTask();
        await Task.Delay(50);
        _smallJumpedTheQueue = _small.IsCompleted;
        _holder.Dispose();
        _largeReservation = await _large.WaitAsync(TimeSpan.FromSeconds(5));
        _largeReservation.Dispose();
        _smallReservation = await _small.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_let_a_small_request_pass_a_large_one_that_is_waiting() => _smallJumpedTheQueue.ShouldBeFalse();
    [Fact] void should_serve_the_large_request_first() => _largeReservation.Bytes.ShouldEqual(800);
    [Fact] void should_serve_the_small_request_after() => _smallReservation.Bytes.ShouldEqual(100);
}
