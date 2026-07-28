// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_another_client_instance_subscribes_with_different_event_types : given.an_observer
{
    static readonly EventType _firstEventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    static readonly EventType _secondEventType = new("87e51e33-1e9c-4c66-a4a6-3a1372c7ecd6", EventTypeGeneration.First);
    ConnectedClient _firstClient;
    ConnectedClient _secondClient;
    ObserverSubscription _subscription;

    async Task Establish()
    {
        _firstClient = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _secondClient = new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" };
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_firstEventType], SiloAddress.Zero, _firstClient);
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_secondEventType], SiloAddress.Zero, _secondClient);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_replace_the_subscription_with_a_single_target() => _subscription.Targets.Count.ShouldEqual(1);
    [Fact] void should_only_have_the_new_client_instance() => _subscription.Targets[0].ConnectedClient!.ConnectionId.ShouldEqual(_secondClient.ConnectionId);
    [Fact] void should_use_the_new_event_types() => _subscription.EventTypes.ShouldContainOnly(_secondEventType);
}
