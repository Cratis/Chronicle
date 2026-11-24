// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_UnpartitionedGates;

public class when_leases_are_released : Specification
{
    UnpartitionedGates _gates;
    int _countWhileOneLeaseIsLeft;
    int _countAtTheEnd;
    int _countAfterDisposingTwice;

    void Establish() => _gates = new();

    void Because()
    {
        var first = _gates.Acquire("subscriber");
        var second = _gates.Acquire("subscriber");
        using var other = _gates.Acquire("other");
        first.Dispose();
        _countWhileOneLeaseIsLeft = _gates.Count;
        second.Dispose();
        second.Dispose();
        _countAfterDisposingTwice = _gates.Count;
        other.Dispose();
        _countAtTheEnd = _gates.Count;
    }

    [Fact] void should_keep_the_gate_while_a_lease_remains() => _countWhileOneLeaseIsLeft.ShouldEqual(2);
    [Fact] void should_remove_the_gate_with_the_last_lease() => _countAfterDisposingTwice.ShouldEqual(1);
    [Fact] void should_end_up_empty() => _countAtTheEnd.ShouldEqual(0);
}
