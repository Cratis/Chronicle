// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class while_a_webhook_serves_the_observer : given.a_reducer_waiting_for_a_webhook
{
    Task Because() => _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] void should_not_compare_definitions() => _definitionComparer.DidNotReceiveWithAnyArgs().Compare(default!, default!, default!);
    [Fact] async Task should_not_replay() => await _observer.DidNotReceive().Replay();
    [Fact] async Task should_not_unsubscribe() => await _observer.DidNotReceive().Unsubscribe();
    [Fact] async Task should_not_subscribe() => await _observer.DidNotReceiveWithAnyArgs().Subscribe<IReducerObserverSubscriber>(default, default!, default!, default, default, default);
    [Fact] void should_keep_the_persisted_definition() => _stateStorage.State.ShouldEqual(_storedDefinition);
    [Fact] async Task should_not_write_state() => await _stateStorage.DidNotReceive().WriteStateAsync();
}
