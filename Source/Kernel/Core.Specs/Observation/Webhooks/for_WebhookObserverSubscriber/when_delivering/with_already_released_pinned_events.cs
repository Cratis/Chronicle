// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_already_released_pinned_events : given.a_webhook_delivery
{
    async Task Establish()
    {
        _events = await _release.Release("store", [_pin], _schemas, _events);
        _metadata.ClearReceivedCalls();
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));
    [Fact] void should_preserve_the_released_payload() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned"));
    [Fact] void should_not_release_values_twice() => _metadata.ReceivedCalls().ShouldBeEmpty();
}
