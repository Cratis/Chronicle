// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_checking_compatibility;

public class and_the_check_call_fails_otherwise : given.a_connection_that_lost_a_working_kernel
{
    IConnectionService _service;
    Exception _error;

    void Establish()
    {
        _service = Substitute.For<IConnectionService>();
        _service.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(Task.FromException<CompatibilityResponse>(new RpcException(new Status(StatusCode.Internal, "Compatibility check failed."))));
    }

    async Task Because() => _error = await Catch.Exception(() => _connection.CheckCompatibility(_service));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_log_a_warning() => _logger.AtLevel(LogLevel.Warning).Count().ShouldEqual(1);
}
