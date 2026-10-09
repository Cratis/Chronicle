// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing;

public class and_no_definition_exists_in_storage : given.a_comparer_with_storage
{
    ReactorDefinitionCompareResult _result;

    void Establish() => _reactorDefinitionsStorage.Has(_reactorKey.ReactorId).Returns(false);

    async Task Because() => _result = await _comparer.Compare(_reactorKey, DefinitionWithEventTypes("event-type"), DefinitionWithEventTypes("event-type"));

    [Fact] void should_be_new() => _result.ShouldEqual(ReactorDefinitionCompareResult.New);
}
