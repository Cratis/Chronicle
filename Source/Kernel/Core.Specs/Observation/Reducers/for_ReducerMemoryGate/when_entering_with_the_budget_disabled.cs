// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_with_the_budget_disabled : given.a_gate_with_a_budget
{
    protected override MemoryManagement Settings => new() { Enabled = false, MinimumReservationBytes = 1000 };

    async Task Because()
    {
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _partition, StateOfAboutBytes(50_000));
        await _gate.Enter(Scope, _partition);
    }

    [Fact] void should_not_reserve_anything() => _ = _budget.DidNotReceive().Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
}
