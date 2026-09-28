// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projections.when_registering_explicitly;

public class a_declarative_projection_while_connected : given.projections_for_explicit_registration
{
    IProjectionHandler _handler;

    void Establish() => _lifecycle.IsConnected.Returns(true);

    async Task Because() => _handler = await _projections.Register<Inventory>(projection => projection.From<ItemAdded>(), "inventory");

    [Fact] void should_use_the_given_projection_identifier() => _handler.Id.Value.ShouldEqual("inventory");
    [Fact] void should_register_the_read_model_with_the_kernel() => _readModels.Received(1).Register<Inventory>();
    [Fact] void should_send_the_projection_to_the_kernel() => _registrations.Single().Projections.Single().Identifier.ShouldEqual("inventory");
    [Fact] void should_not_claim_the_full_set() => _registrations.Single().FullSet.ShouldBeFalse();
    [Fact] void should_send_it_for_the_event_store() => _registrations.Single().EventStore.ShouldEqual("test-event-store");
}
