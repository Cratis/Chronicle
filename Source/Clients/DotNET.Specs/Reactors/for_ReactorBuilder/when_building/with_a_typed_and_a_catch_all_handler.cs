// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class with_a_typed_and_a_catch_all_handler : given.a_reactor_builder
{
    IReactorDefinition _definition;

    void Establish() => _builder
        .On<OrderPlaced>(_ => { })
        .Subscribe((_, _) => Task.CompletedTask);

    void Because() => _definition = _builder.Build("mixed");

    [Fact] void should_subscribe_to_all_events() => _definition.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_subscribe_to_each_event_type_once() => _definition.EventTypes.Count().ShouldEqual(3);
}
