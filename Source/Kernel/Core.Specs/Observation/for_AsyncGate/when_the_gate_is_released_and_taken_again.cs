// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AsyncGate;

public class when_the_gate_is_released_and_taken_again : Specification
{
    readonly AsyncGate _gate = new();
    Task _again = Task.CompletedTask;

    async Task Because()
    {
        await _gate.WaitAsync();
        _gate.Release();
        _again = _gate.WaitAsync();
    }

    [Fact] void should_admit_the_caller_at_once() => _again.IsCompletedSuccessfully.ShouldBeTrue();
}
