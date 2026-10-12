// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving_registered_delivery;

public class with_a_foreign_event : given.a_registered_delivery<given.RegisteredDeliveryReactor>
{
    Task Because() => ResolveDeliveries();

    [Fact] void should_observe_the_foreign_inbox() => _handler.EventSequenceId.ShouldEqual(new EventSequenceId("inbox-other"));
    [Fact] void should_inject_the_registered_handler_delivery() => _delivery.ShouldEqual(_expected);
    [Fact] void should_use_the_registered_handler_receipt_id() => _delivery.Id.ShouldEqual(_expected.Id);
    [Fact] void should_preserve_the_store() => _delivery.EventStore.ShouldEqual(_context.EventStore);
    [Fact] void should_preserve_the_namespace() => _delivery.Namespace.ShouldEqual(_context.Namespace);
    [Fact] void should_preserve_the_partition() => _delivery.Partition.ShouldEqual(_context.EventSourceId);
    [Fact] void should_preserve_the_position() => _delivery.SequenceNumber.ShouldEqual(_context.SequenceNumber);
    [Fact] void should_preserve_every_component_on_replay() => _redelivery.ShouldEqual(_delivery);
    [Fact] void should_preserve_the_receipt_id_on_replay() => _redelivery.Id.ShouldEqual(_delivery.Id);
    [Fact] void should_keep_the_public_declaration_only_factory_unchanged() => _legacyDelivery.EventSequence.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_use_the_legacy_factory_without_a_store() => _fallbackDelivery.ShouldEqual(_legacyDelivery);
}
