// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class with_a_catch_all_handler : given.a_reactor_builder
{
    IReactorDefinition _definition;

    void Establish() => _builder.Subscribe((_, _) => { });

    void Because() => _definition = _builder.Build("everything");

    [Fact] void should_subscribe_to_all_events() => _definition.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_subscribe_to_every_event_type_the_client_knows() => _definition.EventTypes.ShouldContainOnly(_eventTypes.All);
    [Fact] void should_expose_the_clr_type_of_every_event_type() => _definition.ClrTypes.ShouldContainOnly([typeof(OrderPlaced), typeof(OrderShipped), typeof(CustomerRegistered)]);
}
