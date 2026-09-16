// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_omitted_rich_batch_routes : given.an_acknowledged_append
{
    readonly EventForEventSourceId _event = new("source", "first");

    async Task Because() => await _eventSequence.AppendMany([_event, new(_source, "second")]);

    [Fact] void should_keep_public_source_default() => _event.EventSourceType.ShouldEqual(EventSourceType.Default);
    [Fact] void should_keep_public_stream_type_default() => _event.EventStreamType.ShouldEqual(EventStreamType.All);
    [Fact] void should_keep_public_stream_id_default() => _event.EventStreamId.Value.ShouldEqual(EventStreamId.Default);
    [Fact] void should_omit_source_types_on_the_wire() => _batchRequest.Events.All(_ => _.EventSourceType.Length == 0).ShouldBeTrue();
    [Fact] void should_omit_stream_types_on_the_wire() => _batchRequest.Events.All(_ => _.EventStreamType.Length == 0).ShouldBeTrue();
    [Fact] void should_omit_stream_ids_on_the_wire() => _batchRequest.Events.All(_ => _.EventStreamId.Length == 0).ShouldBeTrue();
    [Fact] async Task should_keep_query_sentinels_for_custom_scope_resolution() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default);
    [Fact] void should_notify_from_receipts() => _notifications.Select(_ => _.Event.Context.Hash.Value).ShouldEqual(["stored-hash", "stored-hash"]);
}
