// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_RoundRobinObserverSubscriberSelector.when_selecting;

public class and_subscription_has_single_target : Specification
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
            SiloAddress.Zero)
        {
            Targets = [new(SiloAddress.FromParsableString("127.0.0.1:11111@1"), new ConnectedClient { ConnectionId = "a-connection", Version = "1.0.0" })]
        };
    }

    void Because() => _selected = _selector.Select(_subscription, "some-partition");

    [Fact] void should_select_the_single_target() => _selected.ShouldEqual(_subscription.Targets[0]);
}
