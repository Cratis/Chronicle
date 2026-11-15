// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections.Engine;

namespace Cratis.Chronicle.Projections.for_ProjectionsManager.when_registering;

public class and_an_unchanged_set_is_registered_repeatedly : given.a_projections_manager_grain
{
    ProjectionDefinition[] _definitions;
    int _writesAfterFirstRegistration;

    void Establish()
    {
        _definitions = [.. Enumerable.Range(0, 50).Select(index => CreateDefinition($"the-projection-{index}", "the-read-model") with { Version = "1.0" })];
        _readModelDefinitions = [CreateReadModelDefinition("the-read-model")];
        _state.Projections = [.. _definitions];
        _definitionComparer.Compare(Arg.Any<ProjectionKey>(), Arg.Any<ProjectionDefinition>(), Arg.Any<ProjectionDefinition>()).Returns(ProjectionDefinitionCompareResult.Same);
    }

    async Task Because()
    {
        await _grain.Register(_definitions, registrant: "the-app", version: "1.0");
        _writesAfterFirstRegistration = _stateStorage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "WriteStateAsync");
        for (var retry = 0; retry < 200; retry++)
        {
            await _grain.Register(_definitions, registrant: "the-app", version: "1.0");
        }
    }

    [Fact] void should_compare_the_fifty_definitions_only_once() => _definitionComparer.ReceivedWithAnyArgs(50).Compare(default!, default!, default!);
    [Fact] void should_query_retired_observers_only_once() => _observerStates.ReceivedWithAnyArgs(1).GetRetired(default!);
    [Fact] void should_not_repeat_state_writes() => _stateStorage.ReceivedCalls().Count(call => call.GetMethodInfo().Name == "WriteStateAsync").ShouldEqual(_writesAfterFirstRegistration);
}
