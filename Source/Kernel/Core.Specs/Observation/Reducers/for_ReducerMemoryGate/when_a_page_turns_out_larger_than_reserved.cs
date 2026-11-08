// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

#pragma warning disable CA2012 // Verifying a substituted call does not use the value task it returns.

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

public class when_a_page_turns_out_larger_than_reserved : given.a_gate_for_a_page
{
    Memory.MemoryBudget _real;
    long _reservedWhenEntered;
    long _reservedWhileReducing;
    long _expectedCost;
    long _reservedAfter;

    async Task Establish()
    {
        _real = new Memory.MemoryBudget(() => 100_000_000);
        _gate = new(Microsoft.Extensions.Options.Options.Create(new Configuration.ChronicleOptions { Memory = Settings }), _real);
        using var seed = await _gate.Enter(Scope, _first);
        await _gate.Observe(seed, Scope, _first, StateOfAboutBytes(10_000));
    }

    async Task Because()
    {
        var first = StateOfAboutBytes(50_000);
        var second = StateOfAboutBytes(50_000);
        _expectedCost = (ExpandoObjectSize.Estimate(first) + ExpandoObjectSize.Estimate(second)) * 4;
        var ticket = await _gate.EnterPage(Scope, [_first, _second]);
        _reservedWhenEntered = _real.Reserved;
        await _gate.ObservePage(ticket, Scope, [(_first, first), (_second, second)]);
        _reservedWhileReducing = _real.Reserved;
        ticket.Dispose();
        _reservedAfter = _real.Reserved;
    }

    [Fact] void should_have_reserved_for_what_it_remembered_when_entering() => _reservedWhenEntered.ShouldBeGreaterThan(0L);
    [Fact] void should_grow_the_reservation_to_the_states_read() => _reservedWhileReducing.ShouldEqual(_expectedCost);
    [Fact] void should_give_it_all_back_when_the_page_is_done() => _reservedAfter.ShouldEqual(0);
}
