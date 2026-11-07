// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_a_state_turns_out_larger_than_reserved : given.a_gate_with_a_budget
{
    Memory.MemoryBudget _real;
    ReducerMemoryTicket _ticket;
    long _reservedWhileReducing;
    long _reservedAfter;

    void Establish()
    {
        _real = new Memory.MemoryBudget(() => 100_000_000);
        _gate = new(Microsoft.Extensions.Options.Options.Create(new Configuration.ChronicleOptions { Memory = Settings }), _real);
    }

    async Task Because()
    {
        _ticket = await _gate.Enter(Scope, _partition);
        await _gate.Observe(_ticket, Scope, _partition, StateOfAboutBytes(50_000));
        _reservedWhileReducing = _real.Reserved;
        _ticket.Dispose();
        _reservedAfter = _real.Reserved;
    }

    [Fact] void should_account_for_the_state_once_it_has_been_read() => _reservedWhileReducing.ShouldBeGreaterThan(100_000L);
    [Fact] void should_give_it_back_when_the_reduce_is_done() => _reservedAfter.ShouldEqual(0);
}
