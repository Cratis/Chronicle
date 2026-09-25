// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Tools.GrpcCodeGenerator.for_SharedTypeRegistry;
using TestAssembly.Catalog;

namespace Cratis.Chronicle.Tools.GrpcCodeGenerator.for_ServiceImplementationGenerator.when_dispatching_commands_with_body_properties;

[Collection(SharedTypeRegistryCollection.Name)]
public class and_optional_values_are_absent : given.a_service_with_body_properties
{
    RegisterProductWithOptions _command = null!;

    async Task Because() => _command = await GenerateAndDispatch<RegisterProductWithOptions>();

    [Fact] void should_not_opt_in_to_receipts() => _command.IncludeReceipt.ShouldBeFalse();
    [Fact] void should_preserve_the_absent_concept() => _command.Name.ShouldBeNull();
    [Fact] void should_preserve_the_absent_nullable_value() => _command.Enabled.ShouldBeNull();
    [Fact] void should_preserve_the_absent_shared_type() => _command.Details.ShouldBeNull();
}
