// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_connection_epoch_is_already_set : given.a_reactor_grain_with_a_stored_definition
{
    async Task Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(true);
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _clientConnection.ClearReceivedCalls();
    }

    async Task Because() => await _grain.KeepSubscription(_connectionDefinition);

    [Fact] async Task should_not_subscribe_to_the_client_connection_again() => await _clientConnection.DidNotReceive().Subscribe(Arg.Any<GrainId>());
}
