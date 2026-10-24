// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_observer_lost_the_client : given.a_reactor_grain_with_a_stored_definition
{
    void Establish() => _observer.KnowsClient(_client.ConnectionId).Returns(false);

    async Task Because()
    {
        // The observer continues not to know the client, as when restoration is rejected.
        await _grain.KeepSubscription(_connectionDefinition);
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] Task should_restore_the_subscription_with_the_definition_of_its_own_connection_on_every_attempt() => WasRestored(2);
    [Fact] void should_log_the_resolved_registry_client_only_once() => _logger.Messages
        .Count(message => message.Contains("resolved requested connection", StringComparison.Ordinal))
        .ShouldEqual(1);
}
