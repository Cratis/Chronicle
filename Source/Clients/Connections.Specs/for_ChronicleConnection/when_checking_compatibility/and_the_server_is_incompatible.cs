// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_checking_compatibility;

public class and_the_server_is_incompatible : given.a_connection_with_uncertain_details_checking_compatibility
{
    void Establish() => _connectionService.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns(new CompatibilityResponse
    {
        IsCompatible = false,
        Incompatibilities = ["Service 'X' is missing"],
        ServerVersion = "1.0.0",
        ServerProtocolVersion = "1.0"
    });

    Task Because() => CheckCompatibility();

    [Fact] void should_refuse_the_server() => _failure.ShouldBeOfExactType<IncompatibleServerException>();
    [Fact] void should_log_the_incompatibility() => _logger.Messages.ShouldContain(message => message.Contains("does not serve the contracts", StringComparison.Ordinal));
    [Fact] void should_not_expose_user_information() => AssertNothingSensitiveWasExposed();
}
