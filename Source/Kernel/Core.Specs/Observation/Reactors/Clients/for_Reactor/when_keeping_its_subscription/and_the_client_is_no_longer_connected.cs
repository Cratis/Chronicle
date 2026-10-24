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

    async Task Because()
    {
        await _grain.KeepSubscription(_connectionDefinition);
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] Task should_not_restore_the_subscription() => WasRestored(0);
    [Fact] async Task should_still_check_the_registry_on_each_keep() => await _connectedClients.Received(2).IsConnected(_client.ConnectionId);
    [Fact] void should_log_the_missing_registry_client_only_once() => _logger.Messages
        .Count(message => message.Contains("IsConnected is false", StringComparison.Ordinal))
        .ShouldEqual(1);
}
