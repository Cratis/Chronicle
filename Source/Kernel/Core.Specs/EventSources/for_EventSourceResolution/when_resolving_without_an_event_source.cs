// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSources.for_EventSourceResolution;

public class when_resolving_without_an_event_source : given_registered_event_sources
{
    EventSourceType _result;

    async Task Because()
    {
        var resolved = await EventSourceResolution.Resolve(_eventSources, null, "ShoppingCart", "Anything", CorrelationId.New());
        _result = resolved.AsT0;
    }

    [Fact] void should_accept_it_even_when_the_event_source_type_matches_a_definition() => _result.Value.ShouldEqual("ShoppingCart");
    [Fact] void should_not_look_at_the_definitions() => _eventSources.DidNotReceive().Find(Arg.Any<EventSourceName>());
}
