// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.when_delivering;

public class replayed_events_to_an_envelope_webhook : given.a_webhook_delivery
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
            .Select(_ => _ with { Context = _.Context with { ObservationState = EventObservationState.Replay } })
            .ToArray();
    }

    async Task Because() => _deliveryResult = await _subscriber.OnNext(_partition, _events, new(null));

    [Fact] void should_not_send_anything() => _receiver.Payloads.ShouldBeEmpty();
    [Fact] void should_fail_the_batch() => _deliveryResult.State.ShouldEqual(ObserverSubscriberState.Failed);
    [Fact] void should_not_acknowledge_any_event() => _deliveryResult.LastSuccessfulObservation.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_park_the_partition() => _deliveryResult.FailurePolicy.ShouldEqual(WebhookDeliveryFailurePolicy.ParkPartition);
    [Fact] void should_name_the_live_only_rule() => _deliveryResult.ExceptionMessages.ShouldContain(_ => _.Contains("live-only"));
}
