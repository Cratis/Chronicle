// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_hashed_legacy_span_names : given.an_event_sequence_with_identifier_tracing
{
    protected override ChronicleTelemetryOptions CreateTelemetry() => new() { EventSourceIdTracing = EventSourceIdTracing.Hashed, EventSourceIdHashKey = Key };

    async Task Because() => await Append();

    [Fact] void should_keep_the_legacy_operation_name() => _append.OperationName.ShouldEqual("client.event_sequence.append");
    [Fact] void should_keep_the_hash_under_the_legacy_key() => _append.GetTagItem("event_source_id").ShouldEqual(new IdentifierHasher(Key).Hash(SourceId));
    [Fact] void should_not_add_a_shared_identifier_key() => _append.GetTagItem("cratis.event_source.id").ShouldBeNull();
    [Fact] void should_not_expose_the_identifier_when_starting() => _startedTags.Any(_ => Equals(_.Value, SourceId)).ShouldBeFalse();
}
