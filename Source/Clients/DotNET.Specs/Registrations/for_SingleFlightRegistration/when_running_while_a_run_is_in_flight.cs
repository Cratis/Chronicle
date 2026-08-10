// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_SingleFlightRegistration;

public class when_running_while_a_run_is_in_flight : Specification
{
    SingleFlightRegistration _runner;
    TaskCompletionSource _actionGate;
    int _invocations;
    Task _first;
    Task _second;

    void Establish()
    {
        _actionGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _runner = new SingleFlightRegistration(() =>
        {
            Interlocked.Increment(ref _invocations);
            return _actionGate.Task;
        });
    }

    async Task Because()
    {
        _first = _runner.Run();
        _second = _runner.Run();
        _actionGate.SetResult();
        await Task.WhenAll(_first, _second);
    }

    [Fact] void should_share_the_in_flight_run() => _second.ShouldEqual(_first);
    [Fact] void should_only_invoke_the_action_once() => _invocations.ShouldEqual(1);
}
