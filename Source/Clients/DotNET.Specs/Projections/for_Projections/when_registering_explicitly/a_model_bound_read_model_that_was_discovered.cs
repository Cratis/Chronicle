// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_model_bound_read_model_that_was_discovered : given.projections_for_explicit_registration
{
    IProjectionHandler _discovered;
    IProjectionHandler _handler;

    async Task Establish()
    {
        _lifecycle.IsConnected.Returns(true);
        _clientArtifacts.ModelBoundProjections.Returns([typeof(RegisteredCustomer)]);
        await _projections.Discover();
        _discovered = _projections.GetAllHandlers().Single();
    }

    async Task Because() => _handler = await _projections.Register<RegisteredCustomer>();

    [Fact] void should_return_the_discovered_handler() => _handler.ShouldEqual(_discovered);
    [Fact] void should_not_add_a_second_definition() => _projections.Definitions.Count.ShouldEqual(1);
    [Fact] void should_not_send_anything_to_the_kernel() => _registrations.ShouldBeEmpty();
}
