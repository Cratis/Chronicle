// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_ResolvedEventRouting.when_resolving;

public class a_known_stream : given_an_event_source
{
    ResolvedEventRouting _result;

    void Because() => _result = ResolvedEventRouting.Resolve(_eventSources, typeof(for_EventSources.ShoppingCartEventSource), "Items", null, null);

    [Fact] void should_use_the_definition_name_as_event_source_type() => _result.SourceType.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_use_the_stream_name_as_event_stream_type() => _result.StreamType.Value.ShouldEqual("Items");
    [Fact] void should_name_the_event_source() => _result.EventSource.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_use_the_dimensions_of_the_stream() => _result.Dimensions.ShouldEqual(ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType);
}
