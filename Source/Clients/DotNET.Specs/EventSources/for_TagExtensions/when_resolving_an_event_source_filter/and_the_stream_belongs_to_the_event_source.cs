// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_TagExtensions.when_resolving_an_event_source_filter;

public class and_the_stream_belongs_to_the_event_source : given.an_event_source_filter
{
    (Events.EventSourceType EventSourceType, Events.EventStreamType EventStreamType) _result;

    void Because() => _result = typeof(ItemsReactor).GetEventSourceFilter(_eventSources);

    [Fact] void should_use_the_event_source_definition() => _result.EventSourceType.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_use_the_declared_stream() => _result.EventStreamType.Value.ShouldEqual("Items");
}
