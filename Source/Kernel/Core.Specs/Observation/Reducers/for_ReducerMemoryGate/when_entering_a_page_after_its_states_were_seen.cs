// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_a_page_after_its_states_were_seen : given.a_gate_for_a_page
{
    long _expectedCost;

    async Task Establish()
    {
        var first = StateOfAboutBytes(50_000);
        var second = StateOfAboutBytes(30_000);
        _expectedCost = (ExpandoObjectSize.Estimate(first) + ExpandoObjectSize.Estimate(second)) * 4;
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _first, first);
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _second, second);
        _budget.ClearReceivedCalls();
    }

    async Task Because() => await _gate.EnterPage(Scope, [_first, _second, _neverReduced]);

    [Fact] void should_reserve_once_for_the_page() => _ = _budget.Received(1).Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
    [Fact] void should_reserve_the_summed_remembered_sizes_times_the_multiplier() => _ = _budget.Received(1).Reserve(_expectedCost, Arg.Any<CancellationToken>());
}
