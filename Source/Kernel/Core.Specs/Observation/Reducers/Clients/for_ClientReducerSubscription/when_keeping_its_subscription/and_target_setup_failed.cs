// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_keeping_its_subscription;

public class and_target_setup_failed : given.a_client_reducer_subscription_with_a_stored_definition
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(true);
        _observer.IsClientTarget(_client.ConnectionId).Returns(true);
        _observer.IsSubscriptionEstablished().Returns(false);
    }

    Task Because() => _subscription.KeepSubscription(_connectionDefinition);

    [Fact] Task should_retry_restoration() => WasRestored(1);
}
