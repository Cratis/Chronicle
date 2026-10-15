// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Observation.for_ObserverFilters.when_matching;

public class and_there_are_no_filters : Specification
{
    bool _result;

    void Because() => _result = ObserverFilters.None.Matches(given.an_event.With(eventSourceType: "anything", eventStreamType: "anything"));

    [Fact] void should_match() => _result.ShouldBeTrue();
}
