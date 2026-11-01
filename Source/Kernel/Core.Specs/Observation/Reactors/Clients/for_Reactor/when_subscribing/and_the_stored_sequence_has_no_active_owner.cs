// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class and_the_stored_sequence_has_no_active_owner : given.a_reactor_with_a_newer_owner
{
    void Establish() => _owner.GetSubscription().Returns(ObserverSubscription.Unsubscribed);
    async Task Because() => await _grain.SetDefinitionAndSubscribe(_connectionDefinition);

    [Fact] void should_complete_registration() => Subscribed.ShouldBeTrue();
    [Fact] async Task should_write_the_definition() => await _stateStorage.Received(1).WriteStateAsync();
    [Fact] void should_not_recommend() => _recommendations.ReceivedCalls().ShouldBeEmpty();
}
