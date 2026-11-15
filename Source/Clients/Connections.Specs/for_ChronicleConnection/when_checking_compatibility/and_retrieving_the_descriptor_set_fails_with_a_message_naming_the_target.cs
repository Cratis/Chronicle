// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Connections.for_ChronicleConnection.when_checking_compatibility;

public class and_retrieving_the_descriptor_set_fails_with_a_message_naming_the_target : given.a_connection_with_uncertain_details_checking_compatibility
{
    void Establish()
    {
        _connectionService.CheckCompatibility(Arg.Any<CompatibilityRequest>()).Returns<Task<CompatibilityResponse>>(_ => throw new RpcException(new Status(StatusCode.Unimplemented, "unimplemented")));
        _connectionService.GetDescriptorSet().Returns<Task<DescriptorSetResponse>>(
            _ => throw new RpcException(new Status(StatusCode.Unavailable, $"Error connecting to {Username}:{NumericPassword}")));
    }

    Task Because() => CheckCompatibility();

    [Fact] void should_not_refuse_the_server() => _failure.ShouldBeNull();
    [Fact] void should_log_that_the_descriptor_set_could_not_be_retrieved() => _logger.Messages.ShouldContain(message => message.Contains("Failed to retrieve server descriptor set", StringComparison.Ordinal));
    [Fact] void should_not_expose_user_information() => AssertNothingSensitiveWasExposed();
}
