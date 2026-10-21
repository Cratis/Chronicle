// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_client_is_no_longer_connected : given.a_reactor_grain_with_a_stored_definition
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);

        // Evicted for not responding: the observer dropped it on purpose, so there is nothing to restore.
        _connectedClients.IsConnected(_client.ConnectionId).Returns(false);
    }

    Task Because() => _grain.KeepSubscription(_connectionDefinition);

    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
}
