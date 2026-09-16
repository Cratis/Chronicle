// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_persisted_metadata_different_from_the_request : given.an_acknowledged_append
{
    AppendResult _result;

    void Establish()
    {
        _response.Receipt.EventSourceType = "stored-source-type";
        _response.Receipt.EventStreamType = "stored-stream-type";
        _response.Receipt.EventStreamId = "stored-stream-id";
        _response.Receipt.ObservationState = Contracts.Events.EventObservationState.Replay;
        _causationManager.GetCurrentChain().Returns([]);
    }

    async Task Because() => _result = await _eventSequence.Append(_source, "event", "explicit-stream", "explicit-id", "explicit-source", _correlationId, ["requested-tag"], occurred: _occurred, subject: "requested-subject");

    [Fact] void should_preserve_explicit_source_type() => _request.EventSourceType.ShouldEqual("explicit-source");
    [Fact] void should_preserve_explicit_stream_type() => _request.EventStreamType.ShouldEqual("explicit-stream");
    [Fact] void should_preserve_explicit_stream_id() => _request.EventStreamId.ShouldEqual("explicit-id");
    [Fact] void should_use_the_receipt_for_notifications() => _notifications.Single().Event.Context.ShouldEqual(_result.Receipt);
    [Fact] void should_use_the_persisted_source_type() => _result.Receipt.EventSourceType.Value.ShouldEqual("stored-source-type");
    [Fact] void should_use_the_persisted_stream_type() => _result.Receipt.EventStreamType.Value.ShouldEqual("stored-stream-type");
    [Fact] void should_use_the_persisted_stream_id() => _result.Receipt.EventStreamId.Value.ShouldEqual("stored-stream-id");
    [Fact] void should_map_the_source() => _result.Receipt.EventSourceId.ShouldEqual(_source);
    [Fact] void should_map_the_event_type() => _result.Receipt.EventType.ShouldEqual(new EventType("event", EventTypeGeneration.First));
    [Fact] void should_map_the_sequence_number() => _result.Receipt.SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_map_the_persisted_store() => _result.Receipt.EventStore.ShouldEqual(_eventStoreName);
    [Fact] void should_map_the_persisted_namespace() => _result.Receipt.Namespace.ShouldEqual(_namespace);
    [Fact] void should_use_the_persisted_store_for_observer_completion() => _result.EventStore.ShouldEqual(_result.Receipt.EventStore);
    [Fact] void should_use_the_persisted_namespace_for_observer_completion() => _result.EventStoreNamespace.ShouldEqual(_result.Receipt.Namespace);
    [Fact] void should_map_the_persisted_timestamp() => _result.Receipt.Occurred.ShouldEqual((DateTimeOffset)_response.Receipt.Occurred);
    [Fact] void should_map_the_persisted_correlation() => _result.Receipt.CorrelationId.Value.ShouldEqual(_response.Receipt.CorrelationId);
    [Fact] void should_map_the_persisted_subject() => _result.Receipt.Subject.Value.ShouldEqual("stored-subject");
    [Fact] void should_map_the_persisted_tags() => _result.Receipt.Tags.Select(_ => _.Value).ShouldEqual(["stored-tag"]);
    [Fact] void should_map_the_persisted_hash() => _result.Receipt.Hash.Value.ShouldEqual("stored-hash");
    [Fact] void should_send_empty_request_causation() => _request.Causation.ShouldBeEmpty();
    [Fact] void should_send_the_request_identity() => _request.CausedBy.Subject.ShouldEqual("caller");
    [Fact] void should_map_the_persisted_cause_count() => _result.Receipt.Causation.Count().ShouldEqual(1);
    [Fact] void should_map_the_persisted_cause_type() => _result.Receipt.Causation.Single().Type.Value.ShouldEqual("HTTP");
    [Fact] void should_map_the_persisted_cause_timestamp() => _result.Receipt.Causation.Single().Occurred.ShouldEqual((DateTimeOffset)_response.Receipt.Causation.Single().Occurred);
    [Fact] void should_map_the_persisted_cause_properties() => _result.Receipt.Causation.Single().Properties.ShouldEqual(_response.Receipt.Causation.Single().Properties);
    [Fact] void should_map_the_persisted_identity_chain() => _result.Receipt.CausedBy.ShouldEqual(new Identity("stored-caller", "Stored Caller", "stored-user", new("original-caller", "Original Caller", "original-user")));
    [Fact] void should_map_the_persisted_observation_state() => _result.Receipt.ObservationState.ShouldEqual(EventObservationState.Replay);
    [Fact] async Task should_preserve_explicit_custom_scope_inputs() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, "explicit-stream", "explicit-id", "explicit-source", default);
}
