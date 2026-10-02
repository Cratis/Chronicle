// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_ResolvedEventRouting.when_resolving;

public class no_stream : given_an_event_source
{
    ResolvedEventRouting _result;

    void Because() => _result = ResolvedEventRouting.Resolve(_eventSources, typeof(for_EventSources.ShoppingCartEventSource), null, null, null);

    [Fact] void should_use_all_streams() => _result.StreamType.ShouldEqual(Events.EventStreamType.All);
    [Fact] void should_use_the_dimensions_of_the_event_source() => _result.Dimensions.ShouldEqual(ConcurrencyDimensions.EventSourceId);
}
