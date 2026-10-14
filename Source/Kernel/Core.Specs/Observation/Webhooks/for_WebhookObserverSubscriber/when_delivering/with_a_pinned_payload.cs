// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_pinned_payload : given.a_webhook_delivery
{
    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_keep_the_http_envelope() => _receiver.Payloads.Single().ShouldEqual(Fixture("pinned"));
    [Fact] void should_acknowledge_the_whole_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
