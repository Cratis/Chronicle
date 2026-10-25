// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing;

public class definitions_with_different_event_types : given.a_reactor_definition_comparer
{
    void Establish() => _second = DefinitionWith(new EventType("OtherEvent", EventTypeGeneration.First));

    async Task Because() => await Compare();

    [Fact] void should_be_different() => _result.ShouldEqual(ReactorDefinitionCompareResult.Different);
    [Fact] async Task should_not_read_the_stored_definition() => await _reactorsStorage.DidNotReceive().Get(Arg.Any<ReactorId>());
}
