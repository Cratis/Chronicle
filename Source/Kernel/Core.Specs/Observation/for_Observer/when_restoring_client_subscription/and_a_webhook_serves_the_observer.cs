// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_a_webhook_serves_the_observer : given.a_webhook_observer_with_a_waiting_client
{
    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, ["Event1", "Event2"], SiloAddress.Zero, _client);

    [Fact] async Task should_keep_the_webhook_subscriber() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(IWebhookObserverSubscriber));
    [Fact] async Task should_keep_the_targets_empty() => (await _observer.GetSubscription()).Targets.ShouldBeEmpty();
    [Fact] async Task should_not_know_the_waiting_client() => (await _observer.KnowsClient(_client.ConnectionId)).ShouldBeFalse();
}
