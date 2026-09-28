// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_model_bound_read_model : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;

    async Task Because() => _handler = await _projections.Register<RegisteredCustomer>();

    [Fact] void should_know_the_read_model() => _projections.HasFor<RegisteredCustomer>().ShouldBeTrue();
    [Fact] void should_identify_it_as_a_model_bound_projection_would_be() => _handler.Id.Value.ShouldEqual(typeof(RegisteredCustomer).FullName);
    [Fact] void should_build_the_definition_from_the_attributes() => _projections.Definitions.Single().From.Keys.Single().Id.ShouldEqual(nameof(CustomerRegistered));
}
