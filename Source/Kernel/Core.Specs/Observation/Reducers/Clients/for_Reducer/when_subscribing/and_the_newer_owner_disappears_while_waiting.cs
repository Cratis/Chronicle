// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_Reducer.when_subscribing;

public class and_the_newer_owner_disappears_while_waiting : given.a_reducer_with_a_newer_owner
{
    async Task Because()
    {
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
        await _grain.KeepSubscription(_connectionDefinition);
        await _silo.TimerRegistry.FireAllAsync();
    }

    [Fact] void should_complete_registration() => Subscribed.ShouldBeTrue();
    [Fact] async Task should_write_the_definition() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] async Task should_subscribe_the_observer() => await WasSubscribed(_connectionDefinition);
}
