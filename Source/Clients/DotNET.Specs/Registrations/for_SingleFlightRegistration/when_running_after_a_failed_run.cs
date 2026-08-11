// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations.for_SingleFlightRegistration;

public class when_running_after_a_failed_run : Specification
{
    SingleFlightRegistration _runner;
    List<TimeSpan> _requestedDelays;
    int _invocations;
    Exception _firstRunFailure;

    void Establish()
    {
        _requestedDelays = [];
        _runner = new SingleFlightRegistration(
            () => _invocations++ == 0 ? Task.FromException(new FirstRunFailed()) : Task.CompletedTask,
            new RegistrationBackoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(8), () => 1),
            toWait =>
            {
                _requestedDelays.Add(toWait);
                return Task.CompletedTask;
            });
    }

    async Task Because()
    {
        _firstRunFailure = await Catch.Exception(_runner.Run);
        await _runner.Run();
        await _runner.Run();
    }

    [Fact] void should_surface_the_failure_to_the_caller() => _firstRunFailure.ShouldBeOfExactType<FirstRunFailed>();
    [Fact] void should_back_off_before_the_run_that_follows_the_failure() => _requestedDelays.ShouldContainOnly(TimeSpan.FromSeconds(1));
    [Fact] void should_stop_backing_off_once_a_run_succeeded() => _requestedDelays.Count.ShouldEqual(1);

    /// <summary>
    /// The exception the first run fails with in this specification.
    /// </summary>
    public class FirstRunFailed() : Exception("The first run failed");
}
