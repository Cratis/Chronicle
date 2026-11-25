// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_AsyncGate;

public class when_a_holder_releases_with_a_waiter_queued : Specification
{
    readonly AsyncGate _gate = new();
    Task _first = Task.CompletedTask;
    Task _second = Task.CompletedTask;
    bool _secondWasAdmittedWhileHeld;

    async Task Because()
    {
        await _gate.WaitAsync();
        _first = _gate.WaitAsync();
        _second = _gate.WaitAsync();
        _secondWasAdmittedWhileHeld = _first.IsCompleted;
        _gate.Release();
        await _first.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_not_admit_a_waiter_while_the_gate_is_held() => _secondWasAdmittedWhileHeld.ShouldBeFalse();
    [Fact] void should_admit_the_longest_waiting_caller() => _first.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_keep_the_later_waiter_queued() => _second.IsCompleted.ShouldBeFalse();
}
