// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing;

/// <summary>
/// A sibling namespace's grain for the same reactor id can persist its definition into the shared,
/// namespace-agnostic storage registry before this grain ever receives one of its own - leaving this
/// grain's own state empty while storage reports the reactor as already registered.
/// </summary>
public class and_this_grains_state_is_empty_but_the_reactor_exists_in_storage : given.a_comparer_with_storage
{
    ReactorDefinitionCompareResult _result;
    Exception _exception;

    void Establish() => _reactorDefinitionsStorage.Has(_reactorKey.ReactorId).Returns(true);

    async Task Because() => _exception = await Catch.Exception(async () => _result = await _comparer.Compare(_reactorKey, EmptyDefinition(), DefinitionWithEventTypes("event-type")));

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
    [Fact] void should_be_new() => _result.ShouldEqual(ReactorDefinitionCompareResult.New);
}
