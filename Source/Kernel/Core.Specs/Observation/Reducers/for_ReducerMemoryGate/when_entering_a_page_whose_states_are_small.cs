// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_a_page_whose_states_are_small : given.a_gate_for_a_page
{
    protected override MemoryManagement Settings => new() { MinimumReservationBytes = 100_000, ReducerCostMultiplier = 4 };

    async Task Establish()
    {
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _first, StateOfAboutBytes(10));
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _second, StateOfAboutBytes(10));
    }

    async Task Because() => await _gate.EnterPage(Scope, [_first, _second]);

    [Fact] void should_not_make_it_wait() => _ = _budget.DidNotReceive().Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
}
