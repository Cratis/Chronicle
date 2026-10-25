// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class after_the_definition_changes_while_waiting_for_a_webhook : given.a_reducer_waiting_for_a_webhook
{
    ReducerDefinition _latestDefinition;

    async Task Establish()
    {
        _latestDefinition = _connectionDefinition with { EventTypes = _storedDefinition.EventTypes, ReadModel = "the-updated-read-model" };
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        await _grain.SetDefinitionAndSubscribe(_latestDefinition);
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
    }

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] void should_save_the_latest_definition() => _stateStorage.State.ShouldEqual(_latestDefinition);
    [Fact] Task should_persist_the_latest_registration() => _stateStorage.Received(1).WriteStateAsync();
    [Fact] Task should_register_the_latest_definition() => _definitionComparer.Received(1).Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), _latestDefinition);
    [Fact] Task should_not_register_the_superseded_definition() => _definitionComparer.DidNotReceive().Compare(Arg.Any<ReducerKey>(), Arg.Any<ReducerDefinition>(), _connectionDefinition);
    [Fact] Task should_subscribe_using_the_latest_definition() => WasSubscribed(_latestDefinition);
    [Fact] Task should_not_only_restore_the_subscription() => WasRestored(0);
}
