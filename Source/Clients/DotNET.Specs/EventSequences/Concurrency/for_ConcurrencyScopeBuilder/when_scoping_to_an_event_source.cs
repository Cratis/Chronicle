// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.Concurrency.for_ConcurrencyScopeBuilder;

public class when_scoping_to_an_event_source : Specification
{
    ConcurrencyScope _scope;

    void Because() => _scope = new ConcurrencyScopeBuilder()
        .ForEventSource<EventSources.for_EventSources.ShoppingCartEventSource>("Items", "stream-1")
        .Build();

    [Fact] void should_use_the_event_source_type() => _scope.EventSourceType!.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_use_the_stream_type() => _scope.EventStreamType!.Value.ShouldEqual("Items");
    [Fact] void should_use_the_stream_id() => _scope.EventStreamId!.Value.ShouldEqual("stream-1");
}
