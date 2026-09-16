// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_metadata_is_explicitly_empty : given.an_event_sequence_with_metadata
{
    async Task Because() => await _eventSequence.Append(_source, "event", new EventStreamType(string.Empty), EventStreamId.NotSet, EventSourceType.Unspecified);

    [Fact] void should_send_empty_source_type() => _request.EventSourceType.ShouldEqual(string.Empty);
    [Fact] void should_send_empty_stream_type() => _request.EventStreamType.ShouldEqual(string.Empty);
    [Fact] void should_send_empty_stream_id() => _request.EventStreamId.ShouldEqual(string.Empty);
    [Fact] async Task should_resolve_concurrency_using_the_same_metadata() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default);
    [Fact] void should_notify_once() => _notifications.Length.ShouldEqual(1);
    [Fact] void should_notify_with_the_stored_source_type() => _notifications[0].Event.Context.EventSourceType.Value.ShouldEqual("Default");
    [Fact] void should_notify_with_the_stored_stream_type() => _notifications[0].Event.Context.EventStreamType.Value.ShouldEqual("All");
    [Fact] void should_notify_with_the_stored_stream_id() => _notifications[0].Event.Context.EventStreamId.Value.ShouldEqual("Default");
}
