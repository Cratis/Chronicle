// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_the_definition_changes;

public class and_the_read_model_changes : given.a_reducer_grain_with_a_stored_definition
{
    void Establish()
    {
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>()).Returns(ReducerDefinitionCompareResult.Different);
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition with { ReadModel = "another-read-model" });
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_write_the_definition_before_subscribing() => Received.InOrder(() =>
    {
        _stateStorage.WriteStateAsync();
        SubscribeOn(_observer);
    });

    [Fact] void should_replay_only_after_subscribing() => Received.InOrder(() =>
    {
        SubscribeOn(_observer);
        _observer.Replay();
    });
}
