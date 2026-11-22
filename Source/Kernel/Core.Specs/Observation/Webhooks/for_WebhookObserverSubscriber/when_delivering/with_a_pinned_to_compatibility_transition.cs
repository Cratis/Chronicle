// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class with_a_pinned_to_compatibility_transition : given.a_webhook_delivery
{
    async Task Establish()
    {
        _webhookDefinition = _webhookDefinition with { GenerationDelivery = EventGenerationDelivery.Compatibility };
        _events = (await new EventCompliance(_metadata, _converter, new OwnGenerationSchemaCache()).Release(_events, _schemas)).ToArray();
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(EventGenerationDelivery.Pinned));

    [Fact] void should_not_retain_the_cached_pinned_policy() => _receiver.Payloads.Single().ShouldEqual(Fixture("compatibility"));
    [Fact] void should_acknowledge_the_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
