// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookObserverSubscriber.given;

public class a_compatibility_subscription_with_a_pinned_definition : a_webhook_delivery
{
    async Task Establish()
    {
        _silo.StorageManager.GetStorage<WebhookDefinition>(typeof(WebhookObserverSubscriber).FullName).State = _webhookDefinition with
        {
            GenerationDelivery = EventGenerationDelivery.Compatibility,
            EventTypes = [new(_pin.Id, 1)]
        };
        var schemas = new Dictionary<EventType, Concepts.EventTypes.EventTypeSchema>
        {
            [new(_pin.Id, 1)] = new(new(_pin.Id, 1), EventTypeOwner.Client, EventTypeSource.Code, _firstSchema)
        };
        _events = (await new EventCompliance(_metadata, _converter).Release(_events, schemas)).ToArray();
    }
}
