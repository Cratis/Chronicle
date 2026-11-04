// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.for_Observer.when_unsubscribing_client_instance;

public class and_a_webhook_serves_the_observer : given.an_observer
{
    Task Establish() => _observer.Subscribe<IWebhookObserverSubscriber>(ObserverType.External, ["Event1", "Event2"], SiloAddress.Zero, null, true);

    Task Because() => _observer.UnsubscribeIfMatchesClient("a-connection");

    [Fact] async Task should_keep_the_webhook_subscription() => (await _observer.IsSubscribed()).ShouldBeTrue();
}
