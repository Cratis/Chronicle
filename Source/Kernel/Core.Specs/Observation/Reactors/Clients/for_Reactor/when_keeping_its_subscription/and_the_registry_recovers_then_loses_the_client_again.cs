// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_registry_recovers_then_loses_the_client_again : given.a_reactor_grain_with_a_stored_definition
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
        _connectedClients.IsConnected(_client.ConnectionId).Returns(false, false, true, true, false, false);
    }

    async Task Because()
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            await _grain.KeepSubscription(_connectionDefinition);
        }
    }

    [Fact] void should_log_both_losses() => _logger.Messages
        .Count(message => message.Contains("IsConnected is false", StringComparison.Ordinal))
        .ShouldEqual(2);
    [Fact] void should_log_recovery_only_once() => _logger.Messages
        .Count(message => message.Contains("resolved requested connection", StringComparison.Ordinal))
        .ShouldEqual(1);
    [Fact] async Task should_check_the_registry_on_every_attempt() => await _connectedClients.Received(6).IsConnected(_client.ConnectionId);
    [Fact] Task should_attempt_restoration_on_each_connected_attempt() => WasRestored(2);
}
