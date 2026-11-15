// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_for_a_partition_never_reduced : given.a_gate_with_a_budget
{
    ReducerMemoryTicket _ticket;

    async Task Because() => _ticket = await _gate.Enter(Scope, _partition);

    [Fact] void should_not_reserve_anything() => _ = _budget.DidNotReceive().Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
    [Fact] void should_hold_nothing() => _ticket.Reservation.Bytes.ShouldEqual(0);
}
