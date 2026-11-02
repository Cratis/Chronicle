// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class live_events_to_an_envelope_webhook : given.a_webhook_delivery
{
    async Task Establish()
    {
        _webhookDefinition = _webhookDefinition with { GenerationDelivery = EventGenerationDelivery.Compatibility, DeliveryFormat = WebhookDeliveryFormat.EnvelopeV1 };
        _silo.StorageManager.GetStorage<WebhookDefinition>(typeof(WebhookObserverSubscriber).FullName).State = _webhookDefinition;
        var schemas = new Dictionary<EventType, Concepts.EventTypes.EventTypeSchema>
        {
            [new(_pin.Id, 1)] = new(new(_pin.Id, 1), EventTypeOwner.Client, EventTypeSource.Code, _firstSchema)
        };
        _events = (await new EventCompliance(_metadata, _converter).Release(_events, schemas))
            .Select(_ => _ with { Context = _.Context with { ObservationState = EventObservationState.None } })
            .ToArray();
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_send_one_request() => _receiver.Payloads.Count.ShouldEqual(1);
    [Fact] void should_acknowledge_the_whole_batch() => _deliveryResult.LastSuccessfulObservation.Value.ShouldEqual(44ul);
}
