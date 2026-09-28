// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class with_typed_handlers : given.a_reactor_builder
{
    IReactorDefinition _definition;

    void Establish() => _builder
        .On<OrderPlaced>(_ => { })
        .On<OrderShipped>((_, _) => Task.CompletedTask)
        .On<OrderPlaced>(_ => Task.CompletedTask);

    void Because() => _definition = _builder.Build("orders");

    [Fact] void should_carry_the_identifier() => _definition.Id.Value.ShouldEqual("orders");
    [Fact] void should_subscribe_to_the_handled_event_types_once_each() => _definition.EventTypes.ShouldContainOnly([typeof(OrderPlaced).GetEventType(), typeof(OrderShipped).GetEventType()]);
    [Fact] void should_expose_the_handled_clr_types() => _definition.ClrTypes.ShouldContainOnly([typeof(OrderPlaced), typeof(OrderShipped)]);
    [Fact] void should_not_subscribe_to_all_events() => _definition.SubscribesToAllEvents.ShouldBeFalse();
    [Fact] void should_observe_the_event_log() => _definition.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_be_replayable() => _definition.IsReplayable.ShouldBeTrue();
}
