// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3948 — while the kernel stays unreachable, every
/// caller used to pay a full connect attempt of its own, on its own thread, with no bound on how many could be
/// waiting at once. Only one attempt per back-off window should reach the kernel; the rest fail at once.
/// </summary>
public class and_a_recent_attempt_already_failed : given.a_connection_that_lost_a_working_kernel
{
    Exception _result;

    async Task Because()
    {
        await ConnectThenLoseTheKernel();
        await Catch.Exception(_connection.Connect);
        _result = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_fail_the_call() => _result.ShouldNotBeNull();
    [Fact] void should_report_the_connection_as_unavailable() => _result.ShouldBeOfExactType<ConnectionUnavailable>();
    [Fact] void should_not_attempt_to_connect_again() => _resolveAttempts.ShouldEqual(1);
}
