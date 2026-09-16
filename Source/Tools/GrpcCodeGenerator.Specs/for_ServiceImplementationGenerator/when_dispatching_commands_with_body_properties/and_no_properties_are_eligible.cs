// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.when_dispatching_commands_with_body_properties;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_no_properties_are_eligible : given.a_service_with_body_properties
{
    InspectReceipt _command = null!;

    async Task Because() => _command = await GenerateAndDispatch<InspectReceipt>();

    [Fact] void should_not_generate_a_request() => _hasRequest.ShouldBeFalse();
    [Fact] void should_not_generate_a_request_message() => _contractCode.Contains("InspectReceiptRequest", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_still_dispatch_the_command() => _command.ShouldNotBeNull();
}
