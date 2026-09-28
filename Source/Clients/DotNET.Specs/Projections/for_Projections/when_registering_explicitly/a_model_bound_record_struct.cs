// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_model_bound_record_struct : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;

    async Task Because() => _handler = await _projections.Register<RegisteredCustomerStruct>();

    [Fact] void should_register_the_read_model() => _projections.HasFor<RegisteredCustomerStruct>().ShouldBeTrue();
    [Fact] void should_use_the_discovery_identifier() => _handler.Id.Value.ShouldEqual(typeof(RegisteredCustomerStruct).FullName);
    [Fact] void should_build_the_definition_from_the_attributes() => _projections.Definitions.Single().From.Keys.Single().Id.ShouldEqual(nameof(CustomerRegistered));
}
