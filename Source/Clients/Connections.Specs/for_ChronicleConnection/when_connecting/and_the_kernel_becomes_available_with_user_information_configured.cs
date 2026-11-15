// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_kernel_becomes_available_with_user_information_configured : given.a_connection_with_user_information_that_lost_a_working_kernel
{
    void Establish() => _kernelIsReachable = false;

    async Task Because()
    {
        _connection.HandleConnectTimedOut();
        _kernelIsReachable = true;
        await _connection.Connect();
    }

    [Fact] void should_log_the_startup_warning() => _logger.Messages.ShouldNotBeEmpty();
    [Fact] void should_not_expose_the_password() => _logger.Messages.ShouldNotContain(message => message.Contains("s3cret", StringComparison.Ordinal));
    [Fact] void should_not_expose_the_username() => _logger.Messages.ShouldNotContain(message => message.Contains("//user", StringComparison.Ordinal));
}
