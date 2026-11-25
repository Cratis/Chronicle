// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.Clients.for_ClientReactorSubscription.when_checking_its_subscription;

public class and_target_setup_failed : given.a_client_reactor_subscription_with_a_stored_definition
{
    bool _intact;

    void Establish()
    {
        _observer.IsClientTarget(_client.ConnectionId).Returns(true);
        _observer.IsSubscriptionEstablished().Returns(false);
    }

    async Task Because() => _intact = await _subscription.IsSubscriptionIntact();

    [Fact] void should_not_be_intact() => _intact.ShouldBeFalse();
}
