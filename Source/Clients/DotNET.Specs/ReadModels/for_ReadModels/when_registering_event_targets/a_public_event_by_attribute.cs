// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.ReadModels.for_ReadModels.when_registering_event_targets;

public class a_public_event_by_attribute : given.an_event_target<a_public_event_by_attribute.OrderShipped>
{
    [EventType, Public]
    public record OrderShipped(string Id);

    async Task Because() => await Exercise();

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_register_one_definition() => _request.ReadModels.Count.ShouldEqual(1);
    [Fact] void should_use_the_event_sequence_sink() => _request.ReadModels[0].Sink.TypeId.ShouldEqual("EventSequence");
    [Fact] void should_not_use_a_read_model_sink() => _request.ReadModels[0].Sink.TypeId.ShouldNotEqual(Sinks.WellKnownSinkTypes.MongoDB.Value);
    [Fact] void should_default_to_the_outbox() => _request.ReadModels[0].Sink.EventSequence.EventSequence.ShouldEqual(EventSequences.EventSequenceId.Outbox.Value);
    [Fact] void should_name_the_event_type() => _request.ReadModels[0].Sink.EventSequence.EventType.Id.ShouldEqual(typeof(OrderShipped).GetEventType().Id.Value);
    [Fact] void should_declare_it_public() => _request.ReadModels[0].Sink.EventSequence.IsPublic.ShouldBeTrue();
    [Fact] void should_identify_the_target_as_the_type() => _request.ReadModels[0].Type.Identifier.ShouldEqual(typeof(OrderShipped).FullName);
    [Fact] void should_link_the_projection() => _request.ReadModels[0].ObserverIdentifier.ShouldEqual("a-projection");
}
