// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_another_instance_of_the_client_is_subscribed : given.an_observer_that_lost_its_client
{
    ConnectedClient _otherInstance;
    ObserverSubscription _subscription;

    async Task Establish()
    {
        _otherInstance = new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" };
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _otherInstance);
    }

    async Task Because()
    {
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_target_both_instances() => _subscription.Targets.Select(_ => _.ConnectedClient!.ConnectionId).ShouldContainOnly(_client.ConnectionId, _otherInstance.ConnectionId);
}
