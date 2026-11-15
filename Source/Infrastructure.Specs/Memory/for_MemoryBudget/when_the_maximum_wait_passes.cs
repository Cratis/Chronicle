// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_the_maximum_wait_passes : Specification
{
    MemoryBudget _budget;
    MemoryReservation _holder;
    MemoryReservation _letThrough;

    void Establish() => _budget = new(() => 1000, TimeSpan.FromMilliseconds(100));

    async Task Because()
    {
        _holder = await _budget.Reserve(900);
        _letThrough = await _budget.Reserve(500).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_let_the_reservation_through_regardless() => _letThrough.Bytes.ShouldEqual(500);
    [Fact] void should_account_for_it() => _budget.Reserved.ShouldEqual(1400);
}
