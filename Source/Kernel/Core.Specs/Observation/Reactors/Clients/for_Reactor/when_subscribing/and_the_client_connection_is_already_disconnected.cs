// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_subscribing;

public class and_the_client_connection_is_already_disconnected : given.a_reactor_grain_with_replay_on_definition_change
{
    void Establish() => _clientConnection.Subscribe(Arg.Any<GrainId>()).Returns(ClientConnectionEpoch.NotSet);

    async Task Because() => await _grain.SetDefinitionAndSubscribe(_definition);

    [Fact] async Task should_unsubscribe_its_observer() => await _observerProbe.Received(1).UnsubscribeIfMatchesClient("the-connection");
}
