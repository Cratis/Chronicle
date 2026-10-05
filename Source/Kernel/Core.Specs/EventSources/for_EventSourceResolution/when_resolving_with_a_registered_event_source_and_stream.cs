// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSources.for_EventSourceResolution;

public class when_resolving_with_a_registered_event_source_and_stream : given_registered_event_sources
{
    EventSourceType _result;

    async Task Because()
    {
        var resolved = await EventSourceResolution.Resolve(_eventSources, new EventSourceName("ShoppingCart"), EventSourceType.Default, "Items", CorrelationId.New());
        _result = resolved.AsT0;
    }

    [Fact] void should_fill_the_event_source_type_from_the_definition() => _result.Value.ShouldEqual("ShoppingCart");
}
