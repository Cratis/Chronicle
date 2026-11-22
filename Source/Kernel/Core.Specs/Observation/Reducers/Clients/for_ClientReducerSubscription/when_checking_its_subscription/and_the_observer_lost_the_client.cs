// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_checking_its_subscription;

public class and_the_observer_lost_the_client : given.a_client_reducer_subscription_with_a_stored_definition
{
    bool _intact;

    void Establish() => _observer.IsClientTarget(_client.ConnectionId).Returns(false);

    async Task Because() => _intact = await _subscription.IsSubscriptionIntact();

    [Fact] void should_not_be_intact() => _intact.ShouldBeFalse();
}
