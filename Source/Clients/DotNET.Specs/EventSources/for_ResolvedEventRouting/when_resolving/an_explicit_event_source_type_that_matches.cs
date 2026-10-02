// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_ResolvedEventRouting.when_resolving;

public class an_explicit_event_source_type_that_matches : given_an_event_source
{
    ResolvedEventRouting _result;

    void Because() => _result = ResolvedEventRouting.Resolve(_eventSources, typeof(for_EventSources.ShoppingCartEventSource), "Items", "ShoppingCart", "Items");

    [Fact] void should_accept_it() => _result.Stream!.Name.ShouldEqual("Items");
}
