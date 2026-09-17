// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class and_metadata_is_explicitly_empty : given.an_event_sequence_with_metadata
{
    async Task Because() => await _eventSequence.Append(_source, "event", new EventStreamType(string.Empty), EventStreamId.NotSet, EventSourceType.Unspecified);

    [Fact] void should_send_default_source_type() => _request.EventSourceType.ShouldEqual(EventSourceType.Default.Value);
    [Fact] void should_send_all_stream_type() => _request.EventStreamType.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_send_default_stream_id() => _request.EventStreamId.ShouldEqual(EventStreamId.Default);
    [Fact] async Task should_resolve_concurrency_using_the_same_metadata() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default);
    [Fact] void should_notify_once() => _notifications.Length.ShouldEqual(1);
    [Fact] void should_notify_with_the_sent_source_type() => _notifications[0].Event.Context.EventSourceType.Value.ShouldEqual(_request.EventSourceType);
    [Fact] void should_notify_with_the_sent_stream_type() => _notifications[0].Event.Context.EventStreamType.Value.ShouldEqual(_request.EventStreamType);
    [Fact] void should_notify_with_the_sent_stream_id() => _notifications[0].Event.Context.EventStreamId.Value.ShouldEqual(_request.EventStreamId);
}
