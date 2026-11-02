// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_the_definition_write_fails;

public class and_the_client_retries : given.a_reducer_with_a_newer_owner
{
    ReducerDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { ReadModel = "another-read-model" };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>())
            .Returns(call => ReferenceEquals(call.ArgAt<ReducerDefinition>(1), call.ArgAt<ReducerDefinition>(2)) ? ReducerDefinitionCompareResult.Same : ReducerDefinitionCompareResult.Different);
        _stateStorage.WriteStateAsync().Returns(Task.FromException(new InvalidOperationException()), Task.CompletedTask);
    }

    async Task Because()
    {
        await Catch.Exception(() => _grain.SetDefinitionAndSubscribe(_definition));
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_subscribe_once() => await SubscribeOn(_observer.Received(1));
    [Fact] async Task should_replay_once() => await _observer.Received(1).Replay();
}
