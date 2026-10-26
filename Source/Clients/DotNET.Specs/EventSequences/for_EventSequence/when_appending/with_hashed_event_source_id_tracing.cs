// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_hashed_event_source_id_tracing : given.an_event_sequence_with_identifier_tracing
{
    protected override ChronicleTelemetryOptions CreateTelemetry() => new() { EventSourceIdTracing = EventSourceIdTracing.Hashed, EventSourceIdHashKey = Key };

    async Task Because() => await Append();

    [Fact] void should_make_only_the_hash_available_when_the_span_starts() => _startedTags.Single(_ => _.Key == "cratis.event_source.id").Value.ShouldEqual(new IdentifierHasher(Key).Hash(SourceId));
    [Fact] void should_use_the_same_hash_for_both_keys() => _append.GetTagItem("cratis.event_source.id").ShouldEqual(_append.GetTagItem("event_source_id"));
    [Fact] void should_include_the_keyed_hash() => _append.GetTagItem("event_source_id").ShouldEqual(new IdentifierHasher(Key).Hash(SourceId));
    [Fact] void should_not_include_the_raw_id_in_any_tag() => _append.TagObjects.Any(_ => Equals(_.Value, SourceId)).ShouldBeFalse();
    [Fact] void should_send_the_original_id_to_the_kernel() => _request.EventSourceId.ShouldEqual(SourceId);
    [Fact] void should_never_generate_the_raw_id_tag() => _startedTags.Any(_ => _.Key == "event_source_id").ShouldBeFalse();
    [Fact] void should_not_start_with_a_raw_identifier() => _startedTags.Any(_ => Equals(_.Value, SourceId)).ShouldBeFalse();
}
