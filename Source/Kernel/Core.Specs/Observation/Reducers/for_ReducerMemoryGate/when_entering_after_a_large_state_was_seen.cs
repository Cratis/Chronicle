// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_after_a_large_state_was_seen : given.a_gate_with_a_budget
{
    long _expectedCost;

    async Task Because()
    {
        var state = StateOfAboutBytes(50_000);
        _expectedCost = ExpandoObjectSize.Estimate(state) * 4;
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _partition, state);
        await _gate.Enter(Scope, _partition);
    }

    [Fact] void should_reserve_the_estimated_size_times_the_multiplier_when_the_state_is_read_and_again_when_entering() => _ = _budget.Received(2).Reserve(_expectedCost, Arg.Any<CancellationToken>());
}
