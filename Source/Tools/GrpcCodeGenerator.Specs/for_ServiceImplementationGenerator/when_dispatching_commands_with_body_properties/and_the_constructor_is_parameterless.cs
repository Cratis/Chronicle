// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.when_dispatching_commands_with_body_properties;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_the_constructor_is_parameterless : given.a_service_with_body_properties
{
    SelectReceipt _command = null!;

    async Task Because() => _command = await GenerateAndDispatch<SelectReceipt>(request => Set(request, "IncludeReceipt", true));

    [Fact] void should_generate_a_request() => _hasRequest.ShouldBeTrue();
    [Fact] void should_dispatch_the_body_property() => _command.IncludeReceipt.ShouldBeTrue();
}
