// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_checking_compatibility;

public class and_the_server_is_unavailable : given.a_connection_that_lost_a_working_kernel
{
    IConnectionService _service;
    Exception _error;

    void Establish()
    {
        _service = Substitute.For<IConnectionService>();
        _service.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(Task.FromException<CompatibilityResponse>(new RpcException(new Status(StatusCode.Unavailable, "Error connecting to subchannel."))));
    }

    async Task Because() => _error = await Catch.Exception(() => _connection.CheckCompatibility(_service));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_log_a_warning_or_error() => _logger.Entries.Where(_ => _.Level >= LogLevel.Warning).ShouldBeEmpty();
    [Fact] void should_log_at_debug() => _logger.AtLevel(LogLevel.Debug).ShouldNotBeEmpty();
    [Fact] void should_leave_capabilities_unknown() => _connection.CapabilitiesAreKnown.ShouldBeFalse();
}
