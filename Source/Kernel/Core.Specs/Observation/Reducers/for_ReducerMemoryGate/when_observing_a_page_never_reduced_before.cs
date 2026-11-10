// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_observing_a_page_never_reduced_before : given.a_gate_for_a_page
{
    long _expectedCost;
    bool _enteringReserved;

    async Task Because()
    {
        var first = StateOfAboutBytes(50_000);
        var second = StateOfAboutBytes(30_000);
        _expectedCost = (ExpandoObjectSize.Estimate(first) + ExpandoObjectSize.Estimate(second)) * 4;
        var ticket = await _gate.EnterPage(Scope, [_first, _second]);
        _enteringReserved = _budget.ReceivedCalls().Any();
        await _gate.ObservePage(ticket, Scope, [(_first, first), (_second, second), (_neverReduced, null)]);
    }

    [Fact] void should_not_reserve_when_entering_without_known_sizes() => _enteringReserved.ShouldBeFalse();
    [Fact] void should_reserve_once_for_the_states_read() => _ = _budget.Received(1).Reserve(_expectedCost, Arg.Any<CancellationToken>());
}
