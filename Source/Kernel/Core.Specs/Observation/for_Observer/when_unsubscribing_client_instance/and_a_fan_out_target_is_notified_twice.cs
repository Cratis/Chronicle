// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_a_fan_out_target_is_notified_twice : given.an_observer
{
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    ConnectedClient _firstClient;
    ConnectedClient _secondClient;
    ConnectedClient _thirdClient;
    ObserverSubscription _subscription;

    async Task Establish()
    {
        _firstClient = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _secondClient = new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" };
        _thirdClient = new ConnectedClient { ConnectionId = "c-connection", Version = "1.0.0" };
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, _firstClient);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, _secondClient);
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], SiloAddress.Zero, _thirdClient);
    }

    async Task Because()
    {
        await _observer.UnsubscribeIfMatchesClient(_firstClient.ConnectionId);
        await _observer.UnsubscribeIfMatchesClient(_firstClient.ConnectionId);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] async Task should_still_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] void should_keep_the_two_other_targets() => _subscription.Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_secondClient.ConnectionId, _thirdClient.ConnectionId);
}
