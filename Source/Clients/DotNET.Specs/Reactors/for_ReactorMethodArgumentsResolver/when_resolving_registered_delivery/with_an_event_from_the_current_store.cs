// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving_registered_delivery;

public class with_an_event_from_the_current_store : given.a_registered_delivery<given.RegisteredDeliveryReactor>
{
    protected override EventStoreName CurrentStore => "other";

    Task Because() => ResolveDeliveries();

    [Fact] void should_observe_the_event_log() => _handler.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_inject_the_registered_handler_delivery() => _delivery.ShouldEqual(_expected);
    [Fact] void should_preserve_every_component_on_replay() => _redelivery.ShouldEqual(_delivery);
    [Fact] void should_preserve_the_receipt_id_on_replay() => _redelivery.Id.ShouldEqual(_delivery.Id);
}
