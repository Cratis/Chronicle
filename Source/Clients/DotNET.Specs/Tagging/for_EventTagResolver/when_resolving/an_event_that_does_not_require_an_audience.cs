// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Specs.Tagging.for_EventTagResolver.when_resolving;

public class an_event_that_does_not_require_an_audience : given.an_event_tag_resolver
{
    IReadOnlyList<NamedTag> _result;

    void Because() => _result = Resolve(new PlainEvent("x"));

    [Fact] void should_not_demand_an_audience() => _result.ShouldBeEmpty();
}
