// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGates;

public class when_opening_a_key : Specification
{
    ProjectionWatchGates _gates;
    ProjectionChangesetNotifierKey _key;
    ProjectionChangesetNotifierKey _otherKey;

    void Establish()
    {
        _gates = new(new SettableTimeProvider());
        _key = new("projection", "store", "namespace");
        _otherKey = new("other-projection", "store", "namespace");
    }

    void Because() => _gates.Open(_key);

    [Fact] void should_open_the_gate_of_the_key() => _gates.GateFor(_key).IsOpen.ShouldBeTrue();
    [Fact] void should_leave_other_gates_closed() => _gates.GateFor(_otherKey).IsOpen.ShouldBeFalse();
}
