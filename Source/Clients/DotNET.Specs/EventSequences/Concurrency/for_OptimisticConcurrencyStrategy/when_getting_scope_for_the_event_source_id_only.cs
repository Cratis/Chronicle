// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.Concurrency.for_OptimisticConcurrencyStrategy;

public class when_getting_scope_for_the_event_source_id_only : given_an_event_sequence
{
    ConcurrencyScope _scope;

    async Task Because() => _scope = await _strategy.GetScope(
        ConcurrencyDimensions.EventSourceId,
        _eventSourceId,
        "Items",
        "stream-1",
        "Cart");

    [Fact] void should_keep_the_event_source_id() => _scope.EventSourceId.ShouldEqual(_eventSourceId);
    [Fact] void should_leave_the_stream_type_out() => _scope.EventStreamType.ShouldBeNull();
    [Fact] void should_leave_the_stream_id_out() => _scope.EventStreamId.ShouldBeNull();
    [Fact] void should_leave_the_event_source_type_out() => _scope.EventSourceType.ShouldBeNull();
}
