// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_SingleFlightRegistration;

public class when_running_again_after_a_completed_run : Specification
{
    SingleFlightRegistration _runner;
    List<TimeSpan> _requestedDelays;
    int _invocations;

    void Establish()
    {
        _requestedDelays = [];
        _runner = new SingleFlightRegistration(
            () =>
            {
                _invocations++;
                return Task.CompletedTask;
            },
            new RegistrationBackoff(jitterSource: () => 1),
            toWait =>
            {
                _requestedDelays.Add(toWait);
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        await _runner.Run();
        await _runner.Run();
    }

    [Fact] void should_invoke_the_action_for_each_run() => _invocations.ShouldEqual(2);
    [Fact] void should_never_wait_when_no_run_has_failed() => _requestedDelays.ShouldBeEmpty();
}
