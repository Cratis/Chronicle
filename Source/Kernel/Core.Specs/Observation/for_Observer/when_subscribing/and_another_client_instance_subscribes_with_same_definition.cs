// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_another_client_instance_subscribes_with_same_definition : given.an_observer
{
    static readonly EventType _eventType = new("d9a13e10-21a4-4cfc-896e-fda8dfeb79bb", EventTypeGeneration.First);
    ConnectedClient _firstClient;
    ConnectedClient _secondClient;
    SiloAddress _firstSilo;
    SiloAddress _secondSilo;
    ObserverSubscription _subscription;

    async Task Establish()
    {
        _firstClient = new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" };
        _secondClient = new ConnectedClient { ConnectionId = "b-connection", Version = "1.0.0" };
        _firstSilo = SiloAddress.FromParsableString("127.0.0.1:11111@1");
        _secondSilo = SiloAddress.FromParsableString("127.0.0.1:11112@1");
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], _firstSilo, _firstClient);
    }

    async Task Because()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [_eventType], _secondSilo, _secondClient);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_have_two_targets() => _subscription.Targets.Count.ShouldEqual(2);
    [Fact] void should_have_both_client_instances_as_targets() => _subscription.Targets.Select(target => target.ConnectedClient!.ConnectionId).ShouldContainOnly(_firstClient.ConnectionId, _secondClient.ConnectionId);
    [Fact] void should_keep_each_instance_on_its_own_silo() => _subscription.Targets.Single(target => target.ConnectedClient!.ConnectionId == _secondClient.ConnectionId).SiloAddress.ShouldEqual(_secondSilo);
    [Fact] void should_mirror_first_target_on_the_subscription() => _subscription.SiloAddress.ShouldEqual(_subscription.Targets[0].SiloAddress);
    [Fact] async Task should_still_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
