// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_an_established_connection_times_out : given.a_connection_that_lost_a_working_kernel
{
    Exception _error;

    async Task Establish() => await ConnectThenLoseTheKernel();

    void Because() => _error = Catch.Exception(_connection.HandleConnectTimedOut);

    [Fact] void should_throw_connection_timed_out() => _error.ShouldBeOfExactType<ConnectionTimedOut>();
    [Fact] void should_log_an_error() => _logger.AtLevel(LogLevel.Error).Count().ShouldEqual(1);
    [Fact] void should_not_log_the_startup_warning() => _logger.AtLevel(LogLevel.Warning).ShouldBeEmpty();
}
