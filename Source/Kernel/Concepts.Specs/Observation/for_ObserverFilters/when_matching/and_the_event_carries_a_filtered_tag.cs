// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverFilters.when_matching;

public class and_the_event_carries_a_filtered_tag : Specification
{
    bool _result;

    void Because() => _result = new ObserverFilters(["audited"]).Matches(given.an_event.With(tags: [new("other"), new("audited")]));

    [Fact] void should_match() => _result.ShouldBeTrue();
}
