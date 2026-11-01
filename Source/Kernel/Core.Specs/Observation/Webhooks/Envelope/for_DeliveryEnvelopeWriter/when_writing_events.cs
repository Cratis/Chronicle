// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope.for_DeliveryEnvelopeWriter;

public class when_writing_events : given.a_delivery_request
{
    DeliveryEnvelope _envelope;
    JsonElement _json;
    JsonElement _context;

    void Because()
    {
        _envelope = _writer.ForEvents(_request);
        _json = JsonDocument.Parse(_writer.Serialize(_envelope)).RootElement;
        _context = _json.GetProperty("batches")[0].GetProperty("events")[0].GetProperty("context");
    }

    [Fact] void should_write_the_exact_envelope_fields() => _json.EnumerateObject().Select(_ => _.Name).ShouldContainOnly("kind", "observerType", "observerId", "eventStore", "namespace", "eventSequenceId", "deliveryId", "batches", "replay", "replayEpoch");
    [Fact] void should_write_the_pinned_context_fields() => _context.EnumerateObject().Select(_ => _.Name).ShouldContainOnly("sequenceNumber", "eventType", "eventSourceType", "eventSourceId", "eventStreamType", "eventStreamId", "occurred", "correlationId", "causation", "causedBy", "tags", "subject", "observationState", "dedupKey");
    [Fact] void should_not_write_storage_coordinates_in_context() => _context.TryGetProperty("eventStore", out _).ShouldBeFalse();
    [Fact] void should_not_write_storage_namespace_in_context() => _context.TryGetProperty("namespace", out _).ShouldBeFalse();
    [Fact] void should_not_write_original_type_metadata() => _context.TryGetProperty("originalEventType", out _).ShouldBeFalse();
    [Fact] void should_write_numeric_sequence_number() => _context.GetProperty("sequenceNumber").GetUInt64().ShouldEqual(42UL);
    [Fact] void should_write_the_delivered_type() => _context.GetProperty("eventType").GetRawText().ShouldEqual("{\"id\":\"customer-created\",\"generation\":2}");
    [Fact] void should_write_lowercase_observation_state() => _context.GetProperty("observationState").GetString().ShouldEqual("initial");
    [Fact] void should_write_lowercase_kind() => _json.GetProperty("kind").GetString().ShouldEqual("events");
    [Fact] void should_write_live_only() => _json.GetProperty("replay").GetBoolean().ShouldBeFalse();
    [Fact] void should_write_explicit_null_replay_epoch() => _json.GetProperty("replayEpoch").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_write_explicit_null_fencing_token() => _json.GetProperty("batches")[0].GetProperty("fencingToken").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_write_explicit_null_initial_state() => _json.GetProperty("batches")[0].GetProperty("initialState").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_preserve_content_property_names() => _json.GetProperty("batches")[0].GetProperty("events")[0].GetProperty("content").GetProperty("Name").GetString().ShouldEqual("Ada");
    [Fact] void should_write_plain_string_identity() => _json.GetProperty("observerId").GetString().ShouldEqual("observer");
    [Fact] void should_use_request_store_for_deduplication() => _context.GetProperty("dedupKey").GetString().ShouldEqual("8c4e79dad7dbbd6fb6364d0feb82b79fcc7875d88f0ce6ff9b0fa551557aa6ac");
    [Fact] void should_preserve_event_key_when_rebatched() => _writer.ForEvents(_request with { Events = [.. _request.Events, _request.Events[0] with { Context = _request.Events[0].Context with { SequenceNumber = new EventSequenceNumber(43) } }] }).Batches[0].Events[0].Context.DedupKey.ShouldEqual(_envelope.Batches[0].Events[0].Context.DedupKey);
    [Fact] void should_change_delivery_id_when_rebatched() => _writer.ForEvents(_request with { Events = [.. _request.Events, _request.Events[0] with { Context = _request.Events[0].Context with { SequenceNumber = new EventSequenceNumber(43) } }] }).DeliveryId.ShouldNotEqual(_envelope.DeliveryId);
    [Fact] void should_keep_delivery_id_with_new_attempt_metadata() => _writer.ForEvents(_request with { DeliverBy = _request.DeliverBy.AddMinutes(1), AccessToken = "fresh-token" }).DeliveryId.ShouldEqual(_envelope.DeliveryId);
    [Fact] void should_not_change_the_shared_options() => _options.PropertyNamingPolicy.ShouldBeNull();
    [Fact] void should_write_deterministic_bytes() => Convert.ToHexString(_writer.Serialize(_envelope)).ShouldEqual(Convert.ToHexString(_writer.Serialize(_envelope)));
}
