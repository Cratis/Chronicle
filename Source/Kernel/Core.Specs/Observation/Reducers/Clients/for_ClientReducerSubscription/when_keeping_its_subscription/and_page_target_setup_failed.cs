// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ClientReducerSubscription.when_keeping_its_subscription;

public class and_page_target_setup_failed : given.a_client_reducer_subscription_with_a_stored_definition
{
    void Establish()
    {
        _observer.KnowsClient(_client.ConnectionId).Returns(true);
        _observer.IsSubscriptionEstablished().Returns(false);
    }

    Task Because() => _subscription.KeepSubscription(_connectionDefinition with { PageDelivery = new PageDeliveryDefinition() });

    [Fact] Task should_retry_with_the_page_subscriber() => _observer.Received(1).RestoreClientSubscription<IReducerPageObserverSubscriber>(ObserverType.Reducer, Arg.Any<EventTypeSet>(), Arg.Any<SiloAddress>(), _client, Arg.Any<bool>(), Arg.Any<ObserverFilters?>(), Arg.Any<EventGenerationDelivery>());
}
