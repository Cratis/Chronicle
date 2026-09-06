// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

/// <summary>
/// The back-off after a failed connect keeps callers off the kernel, but it must not be a terminal state - a
/// kernel that comes back has to be reachable again without restarting the client (#3948).
/// </summary>
public class and_the_back_off_after_a_failed_attempt_has_elapsed : given.a_connection_that_lost_a_working_kernel
{
    Exception _result;

    async Task Because()
    {
        await ConnectThenLoseTheKernel();
        await Catch.Exception(_connection.Connect);
        _time.Advance(TimeSpan.FromSeconds(ConnectTimeoutSeconds + 1));
        _result = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_attempt_to_connect_again() => _resolveAttempts.ShouldEqual(2);
    [Fact] void should_still_fail_while_the_kernel_is_unreachable() => _result.ShouldNotBeNull();
    [Fact] void should_surface_the_actual_failure_rather_than_short_circuiting() => _result.ShouldBeOfExactType<UnableToResolveClientUri>();
}
