// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_entering_after_a_small_state_was_seen : given.a_gate_with_a_budget
{
    async Task Because()
    {
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _partition, StateOfAboutBytes(10));
        await _gate.Enter(Scope, _partition);
    }

    [Fact] void should_not_make_it_wait() => _ = _budget.DidNotReceive().Reserve(Arg.Any<long>(), Arg.Any<CancellationToken>());
}
