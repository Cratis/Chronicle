// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing;

public class and_filter_tags_changed : given.two_definitions
{
    void Establish() => _current = _current with { Filters = new ObserverFilters(["different"], "source", "stream") };

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_recognize_the_changed_definition() => _result.ShouldEqual(ReducerDefinitionCompareResult.Different);
}
