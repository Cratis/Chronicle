// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_user_information_is_configured : given.a_connection_with_user_information_that_lost_a_working_kernel
{
    async Task Because() => await _connection.Connect();

    [Fact] void should_log_the_connection_attempt() => _logger.Messages.ShouldContain(message => message.Contains("Connecting to Chronicle", StringComparison.Ordinal));
    [Fact] void should_not_log_the_password() => _logger.Messages.ShouldNotContain(message => message.Contains("s3cret", StringComparison.Ordinal));
    [Fact] void should_not_log_the_username() => _logger.Messages.ShouldNotContain(message => message.Contains("//user", StringComparison.Ordinal));
}
