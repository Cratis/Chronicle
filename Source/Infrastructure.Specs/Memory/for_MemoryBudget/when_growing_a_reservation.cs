// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_growing_a_reservation : Specification
{
    MemoryBudget _budget;
    MemoryReservation _reservation;
    bool _couldReserveWhileGrown;
    long _reservedAfterRelease;

    void Establish() => _budget = new(() => 1000);

    async Task Because()
    {
        _reservation = await _budget.Reserve(100);
        _reservation.Grow(850);
        _couldReserveWhileGrown = _budget.TryReserve(200, out var other);
        other.Dispose();
        _reservation.Dispose();
        _reservedAfterRelease = _budget.Reserved;
    }

    [Fact] void should_hold_the_grown_size() => _reservation.Bytes.ShouldEqual(950);
    [Fact] void should_make_later_reservations_account_for_it() => _couldReserveWhileGrown.ShouldBeFalse();
    [Fact] void should_give_all_of_it_back() => _reservedAfterRelease.ShouldEqual(0);
}
