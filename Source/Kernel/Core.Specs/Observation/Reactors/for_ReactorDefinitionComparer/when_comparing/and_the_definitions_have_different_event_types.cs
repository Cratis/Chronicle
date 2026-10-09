// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing;

public class and_the_definitions_have_different_event_types : given.a_comparer_with_storage
{
    ReactorDefinitionCompareResult _result;

    void Establish() => _reactorDefinitionsStorage.Has(_reactorKey.ReactorId).Returns(true);

    async Task Because() => _result = await _comparer.Compare(_reactorKey, DefinitionWithEventTypes("first-event-type"), DefinitionWithEventTypes("second-event-type"));

    [Fact] void should_be_different() => _result.ShouldEqual(ReactorDefinitionCompareResult.Different);
}
