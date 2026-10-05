// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverFilters.when_matching;

public class and_the_event_source_type_is_the_filtered_one : Specification
{
    bool _result;

    void Because() => _result = new ObserverFilters([], EventSourceType: "account").Matches(given.an_event.With(eventSourceType: "account"));

    [Fact] void should_match() => _result.ShouldBeTrue();
}
