// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Receivers.for_WebhookBodyDecoder.when_decoding;

public class a_kernel_webhook_body : given.a_kernel_webhook_body
{
    Task Because() => Decode();
    [Fact] void should_decode_successfully() => _error.ShouldBeNull();
    [Fact] void should_keep_the_partition() => _delivery.Partition.ShouldEqual("order-42");
    [Fact] void should_keep_the_event_count() => _delivery.Events.Count.ShouldEqual(1);
    [Fact] void should_keep_the_event_type() => _delivery.Events.Single().Context.EventType.Id.ShouldEqual("orders");
    [Fact] void should_keep_the_delivered_generation() => _delivery.Events.Single().Context.EventType.Generation.ShouldEqual(2U);
    [Fact] void should_keep_the_appended_generation() => _delivery.Events.Single().Context.AppendedGeneration.ShouldEqual(1U);
    [Fact] void should_keep_the_sequence_number() => _delivery.Events.Single().Context.SequenceNumber.ShouldEqual(12UL);
    [Fact] void should_keep_the_released_content() => JsonNode.DeepEquals(JsonNode.Parse(_delivery.Events.Single().Content), JsonNode.Parse("{\"order\":42}")).ShouldBeTrue();
    [Fact] void should_keep_generational_content() => _delivery.Events.Single().GenerationalContent[2].ShouldEqual("{\"orderId\":42}");
    [Fact] void should_keep_the_subject() => _delivery.Events.Single().Context.Subject.ShouldEqual("subject");
    [Fact] void should_keep_the_event_source() => _delivery.Events.Single().Context.EventSource.ShouldEqual("orders");
    [Fact] void should_keep_the_named_tags() => _delivery.Events.Single().Context.NamedTags.Single().Value.ShouldEqual("west");
    [Fact] void should_use_the_target_store() => _delivery.Target.EventStore.Value.ShouldEqual("store");
    [Fact] void should_use_the_target_namespace() => _delivery.Target.Namespace.Value.ShouldEqual("tenant");
    [Fact] void should_have_no_initial_state() => _delivery.InitialState.ShouldBeNull();
    [Fact] void should_leave_the_transport_stream_open() => _body.CanRead.ShouldBeTrue();
}
