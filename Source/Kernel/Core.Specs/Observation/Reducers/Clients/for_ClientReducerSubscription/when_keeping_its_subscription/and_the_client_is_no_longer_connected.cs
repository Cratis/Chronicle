// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_keeping_its_subscription;

/// <summary>
/// Evicted for not responding: the observer dropped it on purpose, so there is nothing to restore.
/// </summary>
public class and_the_client_is_no_longer_connected : given.a_client_reducer_subscription_with_a_stored_definition
{
    void Establish() => _connectedClients.IsConnected(_client.ConnectionId).Returns(false);

    Task Because() => _subscription.KeepSubscription(_connectionDefinition);

    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
}
