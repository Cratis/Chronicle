// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_a_recent_attempt_already_failed_with_user_information_configured : given.a_connection_with_user_information_that_lost_a_working_kernel
{
    Exception _result;

    async Task Because()
    {
        await ConnectThenLoseTheKernel();
        await Catch.Exception(_connection.Connect);
        _result = await Catch.Exception(_connection.Connect);
    }

    [Fact] void should_report_the_connection_as_unavailable() => _result.ShouldBeOfExactType<ConnectionUnavailable>();
    [Fact] void should_not_expose_the_password() => _result.Message.Contains("s3cret", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_not_expose_the_username() => _result.Message.Contains("//user", StringComparison.Ordinal).ShouldBeFalse();
}
