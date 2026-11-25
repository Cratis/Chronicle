// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_UnpartitionedGates;

public class when_two_callers_share_a_gate : Specification
{
    UnpartitionedGates _gates;
    UnpartitionedGates.Lease _first;
    UnpartitionedGates.Lease _second;
    Task _secondEntering;
    bool _secondWaitedForTheFirst;
    bool _secondEnteredAfterwards;

    void Establish()
    {
        _gates = new();
        _first = _gates.Acquire("subscriber");
        _second = _gates.Acquire("subscriber");
    }

    async Task Because()
    {
        await _first.Enter(CancellationToken.None);
        _secondEntering = _second.Enter(CancellationToken.None);
        await Task.Delay(50);
        _secondWaitedForTheFirst = !_secondEntering.IsCompleted;
        _first.Exit();
        await _secondEntering.WaitAsync(TimeSpan.FromSeconds(5));
        _secondEnteredAfterwards = _secondEntering.IsCompletedSuccessfully;
        _second.Exit();
        _first.Dispose();
        _second.Dispose();
    }

    [Fact] void should_hold_the_second_caller_until_the_first_leaves() => _secondWaitedForTheFirst.ShouldBeTrue();
    [Fact] void should_let_the_second_caller_in_afterwards() => _secondEnteredAfterwards.ShouldBeTrue();
    [Fact] void should_retain_nothing_at_the_end() => _gates.Count.ShouldEqual(0);
}
