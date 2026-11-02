// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class with_a_changed_definition_and_a_webhook_wins_the_race : given.a_reducer_with_a_newer_owner
{
    ReducerDefinition _definition;

    void Establish()
    {
        _definition = _connectionDefinition with { ReadModel = "another-read-model" };
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        TheNamespaceExists();
        _definitionComparer.Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), Arg.Any<ReducerDefinition>()).Returns(ReducerDefinitionCompareResult.Different);
        SubscribeOn(_observer).Returns(_ => throw new ObserverServedByAnotherTransport(new ObserverKey("the-reducer", "the-event-store", "the-namespace", EventSequenceId.Log)));
    }

    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_definition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] async Task should_write_the_definition() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_still_replay() => await _observer.Received(1).Replay();
}
