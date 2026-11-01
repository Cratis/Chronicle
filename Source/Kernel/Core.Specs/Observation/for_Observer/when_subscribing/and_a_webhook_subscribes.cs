// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_a_webhook_subscribes : given.an_observer_with_subscription
{
    ObserverSubscription _subscription;

    async Task Because()
    {
        await _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, null, true);
        _subscription = await _observer.GetSubscription();
    }

    [Fact] void should_be_subscribed() => _subscription.IsSubscribed.ShouldBeTrue();
    [Fact] void should_be_served_by_a_webhook() => _subscription.IsServedByWebhook.ShouldBeTrue();
    [Fact] void should_not_be_served_by_a_connected_client() => _subscription.IsServedByConnectedClient.ShouldBeFalse();
    [Fact] void should_have_no_targets() => _subscription.Targets.ShouldBeEmpty();
    [Fact] void should_have_no_arguments() => _subscription.Arguments.ShouldBeNull();
}
