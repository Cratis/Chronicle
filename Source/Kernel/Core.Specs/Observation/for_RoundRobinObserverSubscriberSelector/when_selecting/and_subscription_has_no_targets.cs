// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_RoundRobinObserverSubscriberSelector.when_selecting;

public class and_subscription_has_no_targets : Specification
{
    RoundRobinObserverSubscriberSelector _selector;
    ObserverSubscription _subscription;
    ObserverSubscriberTarget _selected;

    void Establish()
    {
        _selector = new RoundRobinObserverSubscriberSelector();
        _subscription = new ObserverSubscription(
            "9bc2a145-3f4b-4a35-8a58-e6a8a4a0f6dd",
            ObserverKey.NotSet,
            [],
            typeof(NullObserverSubscriber),
            SiloAddress.FromParsableString("127.0.0.1:11111@1"));
    }

    void Because() => _selected = _selector.Select(_subscription, "some-partition");

    [Fact] void should_fall_back_to_the_subscription_silo() => _selected.SiloAddress.ShouldEqual(_subscription.SiloAddress);
    [Fact] void should_not_have_a_connected_client() => _selected.ConnectedClient.ShouldBeNull();
}
