// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3948 — once a connection has worked, a failed
/// attempt must reach its caller as a failure. Reporting success while the lifecycle stayed disconnected left no
/// failure state to observe, so every later call re-entered and paid the attempt again, one blocked thread at a
/// time.
/// </summary>
public class and_the_kernel_cannot_be_reached : given.a_connection_that_lost_a_working_kernel
{
    Exception _result;

    async Task Because()
    {
        await ConnectThenLoseTheKernel();
        _result = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_fail_the_call() => _result.ShouldNotBeNull();
    [Fact] void should_have_attempted_to_connect() => _resolveAttempts.ShouldEqual(1);
    [Fact] void should_not_report_the_connection_as_established() => _isConnected.ShouldBeFalse();
}
