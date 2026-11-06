// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_reserving_more_than_the_whole_capacity : Specification
{
    MemoryBudget _budget;
    MemoryReservation _small;
    Task<MemoryReservation> _huge;
    bool _hugeWaitedForTheSmall;
    MemoryReservation _hugeReservation;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _small = await _budget.Reserve(100);
        _huge = _budget.Reserve(50_000).AsTask();
        _hugeWaitedForTheSmall = !_huge.IsCompleted;
        _small.Dispose();
        _hugeReservation = await _huge.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_not_run_alongside_other_work() => _hugeWaitedForTheSmall.ShouldBeTrue();
    [Fact] void should_run_alone_once_nothing_else_is_reserved() => _hugeReservation.Bytes.ShouldEqual(1000);
}
