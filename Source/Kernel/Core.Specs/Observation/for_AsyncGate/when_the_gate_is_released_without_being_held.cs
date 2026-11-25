// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AsyncGate;

public class when_the_gate_is_released_without_being_held : Specification
{
    readonly AsyncGate _gate = new();
    Exception? _error;

    void Because() => _error = Catch.Exception(_gate.Release);

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
