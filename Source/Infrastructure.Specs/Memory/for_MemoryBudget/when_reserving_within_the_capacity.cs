// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_reserving_within_the_capacity : Specification
{
    MemoryBudget _budget;
    MemoryReservation _reservation;
    long _reservedWhileHeld;
    long _reservedAfterRelease;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _reservation = await _budget.Reserve(400);
        _reservedWhileHeld = _budget.Reserved;
        _reservation.Dispose();
        _reservation.Dispose();
        _reservedAfterRelease = _budget.Reserved;
    }

    [Fact] void should_count_the_reserved_bytes_while_held() => _reservedWhileHeld.ShouldEqual(400);
    [Fact] void should_give_the_bytes_back_once_when_disposed() => _reservedAfterRelease.ShouldEqual(0);
}
