// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DnsClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_connecting;

public class and_the_dns_server_fails_to_answer_for_the_first_connect : given.a_connection_whose_servers_cannot_be_resolved
{
    Exception _error;

    protected override Exception ResolutionFailure => new DnsResponseException("the DNS server did not answer");

    async Task Because() => _error = await Catch.Exception(_connection.Connect);

    [Fact] void should_not_fail_the_host() => _error.ShouldBeNull();
    [Fact] void should_not_report_the_connection_as_connected() => _lifecycle.DidNotReceive().Connected();
    [Fact] void should_warn_that_it_keeps_trying() => _logger.AtLevel(LogLevel.Warning).Any(_ => _.Message.Contains("Could not resolve the Chronicle servers")).ShouldBeTrue();
}
