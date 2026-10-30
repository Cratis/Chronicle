// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class after_the_webhook_leaves : given.a_reactor_waiting_for_a_webhook
{
    async Task Establish()
    {
        _stateStorage.State = null!;
        _definitionComparer.Compare(default!, default!, default!).ReturnsForAnyArgs(ReactorDefinitionCompareResult.New);
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _observer.ClearReceivedCalls();
        _observer.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
    }

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] void should_save_the_first_definition() => _stateStorage.State.ShouldEqual(_connectionDefinition);
    [Fact] Task should_persist_the_registration() => _stateStorage.Received(1).WriteStateAsync();
    [Fact] Task should_register_the_definition() => _definitionComparer.Received(1).Compare(Arg.Any<ReactorKey>(), Arg.Any<ReactorDefinition>(), _connectionDefinition);
    [Fact] Task should_subscribe_the_client() => WasSubscribed(_connectionDefinition);
    [Fact] Task should_not_only_restore_the_subscription() => WasRestored(0);
}
