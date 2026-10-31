// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_kernel_becomes_available : given.a_connection_that_lost_a_working_kernel
{
    void Establish() => _kernelIsReachable = false;

    async Task Because()
    {
        _connection.HandleConnectTimedOut();
        _connection.HandleConnectTimedOut();
        _kernelIsReachable = true;
        await _connection.Connect();
    }

    [Fact] void should_connect() => _isConnected.ShouldBeTrue();
    [Fact] void should_log_one_warning_per_failed_attempt() => _logger.AtLevel(LogLevel.Warning).Count().ShouldEqual(2);
    [Fact] void should_name_the_server_it_waits_for() => _logger.AtLevel(LogLevel.Warning).All(_ => _.Message.Contains(ChronicleConnectionString.Default.ServerAddress.ToString())).ShouldBeTrue();
    [Fact] void should_not_attach_exceptions_to_the_warnings() => _logger.AtLevel(LogLevel.Warning).All(_ => _.Exception is null).ShouldBeTrue();
    [Fact] void should_not_log_any_error() => _logger.AtLevel(LogLevel.Error).ShouldBeEmpty();
    [Fact] void should_log_connected() => _logger.AtLevel(LogLevel.Information).Any(_ => _.Message == "Connected to Chronicle").ShouldBeTrue();
}
