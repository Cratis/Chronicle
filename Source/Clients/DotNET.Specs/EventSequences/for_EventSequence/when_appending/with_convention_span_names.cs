// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_convention_span_names : given.an_event_sequence_with_identifier_tracing
{
    protected override ChronicleTelemetryOptions CreateTelemetry() => new() { SpanNames = ClientSpanNames.Convention };

    async Task Because() => await Append();

    [Fact] void should_start_with_the_convention_operation_name() => _append.OperationName.ShouldEqual("cratis.chronicle.client.event_sequence.append");
    [Fact] void should_use_the_convention_display_name() => _append.DisplayName.ShouldEqual("cratis.chronicle.client.event_sequence.append");
    [Fact] void should_keep_the_legacy_event_store_key() => _append.GetTagItem("event_store_name").ShouldEqual("store");
    [Fact] void should_add_the_shared_event_store_key() => _append.GetTagItem("cratis.event_store.name").ShouldEqual("store");
}
