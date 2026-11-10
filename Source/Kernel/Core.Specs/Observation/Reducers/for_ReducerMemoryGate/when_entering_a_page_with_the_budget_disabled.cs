// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_a_page_with_the_budget_disabled : given.a_gate_for_a_page
{
    protected override MemoryManagement Settings => new() { Enabled = false, MinimumReservationBytes = 1000 };

    async Task Because()
    {
        await _gate.ObservePage(ReducerMemoryTicket.None, Scope, [(_first, StateOfAboutBytes(50_000)), (_second, StateOfAboutBytes(50_000))]);
        var ticket = await _gate.EnterPage(Scope, [_first, _second]);
        await _gate.ObservePage(ticket, Scope, [(_first, StateOfAboutBytes(90_000)), (_second, StateOfAboutBytes(90_000))]);
    }

    [Fact] void should_not_reserve_anything() => _ = _budget.DidNotReceive().Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
}
