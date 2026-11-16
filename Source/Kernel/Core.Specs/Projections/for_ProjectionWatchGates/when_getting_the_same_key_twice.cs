// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionWatchGates;

public class when_getting_the_same_key_twice : Specification
{
    ProjectionWatchGates _gates;
    ProjectionWatchGate _first;
    ProjectionWatchGate _second;

    void Establish() => _gates = new(new SettableTimeProvider());

    void Because()
    {
        _first = _gates.GateFor(new("projection", "store", "namespace"));
        _second = _gates.GateFor(new("projection", "store", "namespace"));
    }

    [Fact] void should_return_the_same_gate() => _second.ShouldBeSame(_first);
}
