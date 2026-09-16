// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_multi_source_metadata_is_empty : given.an_event_sequence_with_metadata
{
    EventForEventSourceId _event;

    void Establish() => _event = new(_source, "event")
    {
        EventSourceType = EventSourceType.Unspecified,
        EventStreamType = new EventStreamType(string.Empty),
        EventStreamId = EventStreamId.NotSet
    };

    async Task Because() => await _eventSequence.AppendMany([_event]);

    [Fact] void should_send_one_event() => _batchRequest.Events.Count().ShouldEqual(1);
    [Fact] void should_send_empty_source_type() => _batchRequest.Events.Single().EventSourceType.ShouldEqual(string.Empty);
    [Fact] void should_send_empty_stream_type() => _batchRequest.Events.Single().EventStreamType.ShouldEqual(string.Empty);
    [Fact] void should_send_empty_stream_id() => _batchRequest.Events.Single().EventStreamId.ShouldEqual(string.Empty);
    [Fact] async Task should_resolve_concurrency_using_the_same_metadata() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default);
    [Fact] void should_notify_with_the_stored_source_type() => _notifications.Single().Event.Context.EventSourceType.Value.ShouldEqual("Default");
    [Fact] void should_notify_with_the_stored_stream_type() => _notifications.Single().Event.Context.EventStreamType.Value.ShouldEqual("All");
    [Fact] void should_notify_with_the_stored_stream_id() => _notifications.Single().Event.Context.EventStreamId.Value.ShouldEqual("Default");
    [Fact] void should_not_mutate_the_callers_event() => _event.EventSourceType.ShouldEqual(EventSourceType.Unspecified);
}
