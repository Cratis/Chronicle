// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerMemoryGate;

/// <summary>
/// The bounds of a reducer page count the states it holds, so their sizes are remembered even when nothing is reserved.
/// </summary>
public class when_a_state_is_read_with_the_budget_disabled : given.a_gate_with_a_budget
{
    long _expectedSize;
    long _remembered;

    protected override MemoryManagement Settings => new() { Enabled = false };

    async Task Because()
    {
        var state = StateOfAboutBytes(50_000);
        _expectedSize = ExpandoObjectSize.Estimate(state);
        await _gate.Observe(ReducerMemoryTicket.None, Scope, _partition, state);
        _remembered = _gate.RememberedSizeOf(Scope, _partition);
    }

    [Fact] void should_remember_the_size_of_the_state() => _remembered.ShouldEqual(_expectedSize);
}
