// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing;

public class definitions_with_different_event_types : given.two_definitions
{
    void Establish() => _current = _current with { EventTypes = [new(new EventType("other-event", 1), WellKnownExpressions.EventSourceId)] };

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_be_different() => _result.ShouldEqual(ReducerDefinitionCompareResult.Different);
    [Fact] async Task should_not_read_the_stored_definition() => await _reducersStorage.DidNotReceive().Get(Arg.Any<ReducerId>());
}
