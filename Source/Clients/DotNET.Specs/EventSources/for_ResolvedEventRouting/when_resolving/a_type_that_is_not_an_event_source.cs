// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources.for_ResolvedEventRouting.when_resolving;

public class a_type_that_is_not_an_event_source : given_an_event_source
{
    Exception _exception;

    void Because() => _exception = Catch.Exception(() => ResolvedEventRouting.Resolve(_eventSources, typeof(string), null, null, null));

    [Fact] void should_reject_it() => _exception.ShouldBeOfExactType<UnknownEventSource>();
}
