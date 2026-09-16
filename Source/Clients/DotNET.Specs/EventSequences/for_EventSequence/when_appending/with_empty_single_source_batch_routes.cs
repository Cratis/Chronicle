// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_empty_single_source_batch_routes : given.an_acknowledged_append
{
    async Task Because() => await _eventSequence.AppendMany(_source, ["first", "second"], new EventStreamType(string.Empty), EventStreamId.NotSet, EventSourceType.Unspecified);

    [Fact] void should_use_the_rich_rpc() => _batchRequest.ShouldNotBeNull();
    [Fact] void should_preserve_empty_source_types() => _batchRequest.Events.Select(_ => _.EventSourceType).ShouldEqual([string.Empty, string.Empty]);
    [Fact] void should_preserve_empty_stream_types() => _batchRequest.Events.Select(_ => _.EventStreamType).ShouldEqual([string.Empty, string.Empty]);
    [Fact] void should_preserve_empty_stream_ids() => _batchRequest.Events.Select(_ => _.EventStreamId).ShouldEqual([string.Empty, string.Empty]);
    [Fact] async Task should_keep_query_sentinels_for_custom_scope_resolution() => await _concurrencyScopeStrategy.Received(1).GetScope(_source, EventStreamType.All, EventStreamId.Default, EventSourceType.Default, default);
}
