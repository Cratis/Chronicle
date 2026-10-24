// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_Reactor.when_keeping_its_subscription;

public class and_the_initial_registration_already_resolved_the_client : given.a_reactor_grain_with_a_stored_definition
{
    async Task Establish()
    {
        await _grain.SetDefinitionAndSubscribe(_connectionDefinition);
        _observer.KnowsClient(_client.ConnectionId).Returns(false);
    }

    async Task Because()
    {
        await _grain.KeepSubscription(_connectionDefinition);
        await _grain.KeepSubscription(_connectionDefinition);
    }

    [Fact] void should_not_repeat_the_initial_resolution_diagnostic() => _logger.Messages
        .Count(message => message.Contains("resolved requested connection", StringComparison.Ordinal))
        .ShouldEqual(1);
    [Fact] Task should_still_attempt_restoration_each_time() => WasRestored(2);
}
