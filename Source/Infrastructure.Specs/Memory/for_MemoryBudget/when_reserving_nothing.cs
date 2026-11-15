// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Memory.for_MemoryBudget;

public class when_reserving_nothing : Specification
{
    MemoryBudget _budget;
    MemoryReservation _reservation;

    void Establish() => _budget = new(() => 10);

    async Task Because() => _reservation = await _budget.Reserve(0);

    [Fact] void should_hold_nothing() => _reservation.Bytes.ShouldEqual(0);
    [Fact] void should_not_be_counted() => _budget.Reserved.ShouldEqual(0);
}
