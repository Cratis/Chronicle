// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing.and_the_local_definition_is_empty;

public class and_the_stored_definition_differs : given.two_definitions
{
    void Establish()
    {
        _reducersStorage.Get(_key.ReducerId).Returns(_previous);
        _previous = _previous with { EventTypes = null! };
        _current = _current with { EventTypes = [new(new EventType("other-event", 1), WellKnownExpressions.EventSourceId)] };
    }

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_be_different() => _result.ShouldEqual(ReducerDefinitionCompareResult.Different);
}
